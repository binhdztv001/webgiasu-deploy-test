using System;
using System.Collections.Generic;
using System.Linq;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _db;
        public PaymentService(AppDbContext db) => _db = db;

        public List<Payment> GetAllPayments() => _db.Payments.OrderByDescending(p => p.CreatedDate).ToList();

        public Payment? GetPaymentById(int id) => _db.Payments.Find(id);

        public Payment? GetPaymentByProblemId(int problemId) => _db.Payments.FirstOrDefault(p => p.ProblemId == problemId);

        public List<Payment> GetPaymentsByStudentId(int studentId) =>
            _db.Payments.Where(p => p.StudentId == studentId).OrderByDescending(p => p.CreatedDate).ToList();

        public bool CreatePayment(Payment payment)
        {
            payment.CreatedDate = DateTime.Now;
            payment.Status = PaymentStatus.Pending;
            _db.Payments.Add(payment);
            _db.SaveChanges();
            return true;
        }

        public bool UpdatePaymentStatus(int paymentId, PaymentStatus status)
        {
            var p = _db.Payments.Find(paymentId);
            if (p == null) return false;
            p.Status = status;
            if (status == PaymentStatus.Completed)
            {
                p.CompletedDate = DateTime.Now;
                p.TransactionId = $"TXN{DateTime.Now.Ticks}";
            }
            _db.SaveChanges();
            return true;
        }

        public decimal GetTotalRevenue() =>
            _db.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);




        // ==================== THANH TOÁN PROBLEM NHÓM ====================

        public GroupPayment? CreateGroupPayment(int groupId, int memberId, int userId, decimal amount)
        {
            try
            {
                // Kiểm tra đã có payment chưa
                var existing = _db.GroupPayments.FirstOrDefault(gp => gp.MemberId == memberId);
                if (existing != null) return existing;

                var groupPayment = new GroupPayment
                {
                    GroupId = groupId,
                    MemberId = memberId,
                    UserId = userId,
                    Amount = amount,
                    Status = PaymentStatus.Pending,
                    CreatedDate = DateTime.Now
                };

                _db.GroupPayments.Add(groupPayment);
                _db.SaveChanges();
                return groupPayment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi tạo thanh toán nhóm: {ex.Message}");
                return null;
            }
        }

        public GroupPayment? GetGroupPaymentById(int id) => _db.GroupPayments.Find(id);

        public GroupPayment? GetGroupPaymentByMemberId(int memberId) =>
            _db.GroupPayments.FirstOrDefault(gp => gp.MemberId == memberId);

        public List<GroupPayment> GetGroupPaymentsByGroupId(int groupId) =>
            _db.GroupPayments.Where(gp => gp.GroupId == groupId)
                .OrderByDescending(gp => gp.CreatedDate).ToList();

        public List<GroupPayment> GetGroupPaymentsByUserId(int userId) =>
            _db.GroupPayments.Where(gp => gp.UserId == userId)
                .OrderByDescending(gp => gp.CreatedDate).ToList();

        public bool UpdateGroupPaymentStatus(int groupPaymentId, PaymentStatus status)
        {
            try
            {
                var gp = _db.GroupPayments.Find(groupPaymentId);
                if (gp == null) return false;

                gp.Status = status;
                if (status == PaymentStatus.Completed)
                {
                    gp.CompletedDate = DateTime.Now;
                    gp.TransactionId = $"GTXN{DateTime.Now.Ticks}";

                    // Cập nhật trạng thái thanh toán của member
                    var member = _db.ProblemGroupMembers.Find(gp.MemberId);
                    if (member != null)
                    {
                        member.PaymentStatus = GroupPaymentStatus.Paid;
                    }
                }

                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi cập nhật trạng thái thanh toán nhóm: {ex.Message}");
                return false;
            }
        }
    }



}
