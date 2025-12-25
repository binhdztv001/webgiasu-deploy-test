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
    }
}
