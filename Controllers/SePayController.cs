using System;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Webgiasu.Models;
using Webgiasu.Services;
using System.Text.Json;

namespace Webgiasu.Controllers
{
    [ApiController]
    [Route("sepay")]
    [IgnoreAntiforgeryToken]
    public class SePayController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ISePayGateway _sePayGateway;
        private readonly SuiService _suiService;
        private readonly NotificationService _notificationService;
        
        public SePayController(AppDbContext db, ISePayGateway sePayGateway, SuiService suiService, NotificationService notificationService)
        {
            _db = db;
            _sePayGateway = sePayGateway;
            _suiService = suiService;
            _notificationService = notificationService;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            IDictionary<string, string>? payload = null;

            if (Request.HasFormContentType)
            {
                var form = Request.Form;
                payload = form.ToDictionary(k => k.Key, v => v.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                        if (dict != null)
                        {
                            payload = dict.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                        }
                    }
                    catch
                    {
                        return BadRequest("Invalid payload");
                    }
                }
            }

            if (payload == null || payload.Count == 0) return BadRequest();

            // Verify signature only if provided (card/checkout flow). Bank webhook often has no signature.
            if (payload.TryGetValue("signature", out var signature))
            {
                if (!_sePayGateway.VerifySignature(payload, signature))
                {
                    return Unauthorized();
                }
            }

            // Extract potential transaction identifiers first
            payload.TryGetValue("transaction_id", out var transactionId);
            if (string.IsNullOrEmpty(transactionId) && payload.TryGetValue("referenceCode", out var refCode))
            {
                transactionId = refCode;
            }
            if (string.IsNullOrEmpty(transactionId) && payload.TryGetValue("id", out var idVal))
            {
                transactionId = idVal;
            }
            if (string.IsNullOrEmpty(transactionId) && payload.TryGetValue("code", out var codeVal))
            {
                transactionId = codeVal;
            }

            // Thử resolve individual payment trước
            var paymentId = ResolvePaymentId(payload);

            if (paymentId == null && !string.IsNullOrEmpty(transactionId))
            {
                paymentId = ResolvePaymentByTransaction(transactionId);
            }
            // Nếu không có, thử group payment
            int? groupPaymentId = null;
            if (paymentId == null)
            {
                groupPaymentId = ResolveGroupPaymentId(payload);
            }

            // Xử lý individual payment
            if (paymentId != null)
            {
                var payment = _db.Payments.FirstOrDefault(p => p.Id == paymentId.Value);
                if (payment is null) return NotFound();

                // Respond with ordered JSON similar to SePay sample
                var response = new
                {
                    id = TryGetInt(payload, "id"),
                    gateway = GetVal(payload, "gateway"),
                    transactionDate = GetVal(payload, "transactionDate"),
                    accountNumber = GetVal(payload, "accountNumber"),
                    code = GetVal(payload, "code"),
                    content = GetVal(payload, "content"),
                    transferType = GetVal(payload, "transferType"),
                    transferAmount = TryGetLong(payload, "transferAmount"),
                    accumulated = TryGetLong(payload, "accumulated"),
                    subAccount = GetVal(payload, "subAccount"),
                    referenceCode = GetVal(payload, "referenceCode"),
                    description = GetVal(payload, "description")
                };

                // Decide success/failure
                var status = payload.TryGetValue("payment_status", out var ps) ? ps : null;
                var transferType = payload.TryGetValue("transferType", out var tt) ? tt : null;
                var isSuccess = string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(transferType, "in", StringComparison.OrdinalIgnoreCase);
                var isFailed = string.Equals(status, "FAILED", StringComparison.OrdinalIgnoreCase);

                if (isSuccess)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.CompletedDate = DateTime.Now;

                    // SUI Blockchain
                    var suiTx = await _suiService.RecordPaymentAsync(
                        payment.Id.ToString(),               // order_id
                        (long)payment.Amount,                // amount
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    );

                    payment.TransactionId = suiTx; // txDigest
                }
                else if (isFailed)
                {
                    payment.Status = PaymentStatus.Failed;
                }

                // Idempotency: if a completed payment already has this transaction id, skip updates
                if (!string.IsNullOrEmpty(transactionId))
                {
                    var dup = _db.Payments.FirstOrDefault(p => p.TransactionId == transactionId);
                    if (dup != null && dup.Status == PaymentStatus.Completed)
                    {
                        return new JsonResult(response);
                    }
                }

                _db.SaveChanges();

                return new JsonResult(response);
            }
            // Xử lý group payment
            else if (groupPaymentId != null)
            {
                var groupPayment = _db.GroupPayments.FirstOrDefault(gp => gp.Id == groupPaymentId.Value);
                if (groupPayment == null) return NotFound();

                // Xác định thành công/thất bại
                var status = payload.TryGetValue("payment_status", out var ps) ? ps : null;
                var transferType = payload.TryGetValue("transferType", out var tt) ? tt : null;
                var isSuccess = string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(transferType, "in", StringComparison.OrdinalIgnoreCase);

                if (isSuccess)
                {
                    // Idempotency: đã hoàn tất thì không ghi lại blockchain
                    if (groupPayment.Status == PaymentStatus.Completed &&
                        !string.IsNullOrEmpty(groupPayment.TransactionId))
                    {
                        return new JsonResult(new { status = "ok" });
                    }

                    groupPayment.Status = PaymentStatus.Completed;
                    groupPayment.CompletedDate = DateTime.Now;

                    // BIÊN LAI BLOCKCHAIN
                    var suiTx = await _suiService.RecordPaymentAsync(
                        groupPayment.Id.ToString(),               // order_id
                        (long)groupPayment.Amount,                // amount
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    );

                    groupPayment.TransactionId = suiTx;

                    // Cập nhật trạng thái member
                    var member = _db.ProblemGroupMembers.Find(groupPayment.MemberId);
                    if (member != null)
                    {
                        member.PaymentStatus = GroupPaymentStatus.Paid;
                    }

                    // ✅ NEW: Thông báo cho user khi thanh toán nhóm thành công
                    _notificationService.NotifyPaymentCompleted(
                        groupPayment.UserId,
                        groupPayment.Id,
                        groupPayment.Amount
                    );

                    // ✅ NEW: Kiểm tra xem tất cả thành viên đã thanh toán chưa
                    var group = _db.ProblemGroups.Find(groupPayment.GroupId);
                    if (group != null)
                    {
                        var allMembers = _db.ProblemGroupMembers.Where(m => m.GroupId == group.Id).ToList();
                        var allPaid = allMembers.All(m => m.PaymentStatus == GroupPaymentStatus.Paid);

                        if (allPaid)
                        {
                            // ✅ Nếu tất cả đã thanh toán, thông báo cho Tutor
                            var problem = _db.Problems.Find(group.ProblemId);
                            if (problem?.AssignedTutorId.HasValue == true)
                            {
                                _notificationService.NotifyTutorPaymentReceived(
                                    problem.AssignedTutorId.Value,
                                    problem.Id,
                                    group.TotalPrice
                                );
                            }
                        }
                    }
                }
                else if (string.Equals(status, "FAILED", StringComparison.OrdinalIgnoreCase))
                {
                    groupPayment.Status = PaymentStatus.Failed;
                }
                _db.SaveChanges();

                var response = new
                {
                    id = TryGetInt(payload, "id"),
                    gateway = GetVal(payload, "gateway"),
                    transactionDate = GetVal(payload, "transactionDate"),
                    code = GetVal(payload, "code"),
                    content = GetVal(payload, "content"),
                    transferAmount = TryGetLong(payload, "transferAmount")
                };

                return new JsonResult(response);
            }
            return Ok(new { status = "ok" });
        }

        private int? ResolvePaymentByTransaction(string? transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) return null;
            var p = _db.Payments.FirstOrDefault(x => x.TransactionId == transactionId);
            return p?.Id;
        }

        private static string? GetVal(IDictionary<string, string> payload, string key)
        {
            return payload.TryGetValue(key, out var val) ? val : null;
        }

        private static int? TryGetInt(IDictionary<string, string> payload, string key)
        {
            return payload.TryGetValue(key, out var val) && int.TryParse(val, out var num) ? num : null;
        }

        private static long? TryGetLong(IDictionary<string, string> payload, string key)
        {
            return payload.TryGetValue(key, out var val) && long.TryParse(val, out var num) ? num : null;
        }

        private int? ResolvePaymentId(IDictionary<string, string> payload)
        {
            // Try direct invoice
            if (payload.TryGetValue("order_invoice_number", out var invoice) && TryParsePayId(invoice, out var pid))
            {
                return pid;
            }

            // Try referenceCode, content, description, order_description, code, transaction_id
            var candidates = new[] { "referenceCode", "content", "description", "order_description", "code", "transaction_id" };
            foreach (var key in candidates)
            {
                if (payload.TryGetValue(key, out var value) && TryParsePayId(value, out pid))
                {
                    return pid;
                }
            }

            // Fallback by amount if invoice markers are missing
            return ResolvePaymentIdByAmount(payload);
        }

        private int? ResolvePaymentIdByAmount(IDictionary<string, string> payload)
        {
            var amount = TryGetLong(payload, "transferAmount") ?? TryGetLong(payload, "order_amount");
            if (amount == null) return null;
            var targetAmount = (decimal)amount.Value;

            var candidate = _db.Payments
                // .Where(p => p.Status == PaymentStatus.Pending && p.Amount == targetAmount)
                .Where(p => p.Amount == targetAmount)
                .OrderByDescending(p => p.CreatedDate)
                .FirstOrDefault();

            return candidate?.Id;
        }

        private bool TryParsePayId(string? value, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var prefix = "LEARNTUTOR-";
            var idx = value.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            var start = idx + prefix.Length;
            var digits = new string(value.Skip(start).TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out id);
        }

        // Thêm phương thức helper
        private int? ResolveGroupPaymentId(IDictionary<string, string> payload)
        {
            var candidates = new[] { "order_invoice_number", "referenceCode", "content", "description", "code" };
            foreach (var key in candidates)
            {
                if (payload.TryGetValue(key, out var value) && TryParseGroupPayId(value, out var gpId))
                {
                    return gpId;
                }
            }
            return null;
        }

        private bool TryParseGroupPayId(string? value, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var prefix = "LEARNTUTOR-";
            var idx = value.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            var start = idx + prefix.Length;
            var digits = new string(value.Skip(start).TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out id);
        }
    }
}
