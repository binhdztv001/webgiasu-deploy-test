using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public sealed class SePayOptions
    {
        public string Env { get; set; } = "sandbox";
        public string MerchantId { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = "https://pay-sandbox.sepay.vn";
        public string CheckoutPath { get; set; } = "/v1/checkout/init";
    }

    public sealed record SePayCheckout(string ActionUrl, IReadOnlyList<KeyValuePair<string, string>> Fields);

    public interface ISePayGateway
    {
        SePayCheckout BuildCheckout(Payment payment, string successUrl, string errorUrl, string cancelUrl, string paymentMethod = "BANK_TRANSFER");
        bool VerifySignature(IDictionary<string, string> fields, string signature);
    }

    public sealed class SePayGateway : ISePayGateway
    {
        private readonly SePayOptions _options;

        public SePayGateway(IOptions<SePayOptions> options)
        {
            _options = options.Value;
        }

        public SePayCheckout BuildCheckout(Payment payment, string successUrl, string errorUrl, string cancelUrl, string paymentMethod = "BANK_TRANSFER")
        {
            var baseUrl = _options.Env.Equals("production", StringComparison.OrdinalIgnoreCase)
                ? "https://pay.sepay.vn"
                : _options.ApiBaseUrl?.TrimEnd('/');
            var actionUrl = baseUrl + _options.CheckoutPath;

            var fields = new Dictionary<string, string>
            {
                ["merchant"] = _options.MerchantId,
                ["currency"] = "VND",
                ["order_amount"] = ((long)payment.Amount).ToString(),
                ["operation"] = "PURCHASE",
                ["payment_method"] = paymentMethod,
                ["order_description"] = $"Thanh toan bai toan {payment.ProblemId}",
                ["order_invoice_number"] = $"LEARNTUTOR{payment.Id}",
                ["customer_id"] = payment.StudentId.ToString(),
                ["success_url"] = successUrl,
                ["error_url"] = errorUrl,
                ["cancel_url"] = cancelUrl
            };

            var signature = Sign(fields);

            var orderedKeys = new[]
            {
                "merchant",
                "currency",
                "order_amount",
                "operation",
                "payment_method",
                "order_description",
                "order_invoice_number",
                "customer_id",
                "success_url",
                "error_url",
                "cancel_url",
                "signature"
            };

            var ordered = new List<KeyValuePair<string, string>>();
            foreach (var key in orderedKeys)
            {
                if (key == "signature")
                {
                    ordered.Add(new KeyValuePair<string, string>("signature", signature));
                    continue;
                }
                if (!fields.TryGetValue(key, out var value)) continue;
                ordered.Add(new KeyValuePair<string, string>(key, value));
            }

            return new SePayCheckout(actionUrl, ordered);
        }

        public bool VerifySignature(IDictionary<string, string> fields, string signature) =>
            string.Equals(Sign(fields), signature, StringComparison.Ordinal);

        private string Sign(IDictionary<string, string> fields)
        {
            var orderedKeys = new[]
            {
                "merchant", "currency", "order_amount", "operation", "payment_method", "order_description",
                "order_invoice_number", "customer_id", "success_url", "error_url", "cancel_url"
            };

            var signedParts = new List<string>();
            foreach (var key in orderedKeys)
            {
                if (!fields.TryGetValue(key, out var value)) continue;
                signedParts.Add($"{key}={value}");
            }

            var payload = string.Join(",", signedParts);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
