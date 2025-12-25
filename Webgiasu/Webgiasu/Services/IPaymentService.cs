using System.Collections.Generic;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface IPaymentService
    {
        List<Payment> GetAllPayments();
        Payment? GetPaymentById(int id);
        Payment? GetPaymentByProblemId(int problemId);
        List<Payment> GetPaymentsByStudentId(int studentId);
        bool CreatePayment(Payment payment);
        bool UpdatePaymentStatus(int paymentId, PaymentStatus status);
        decimal GetTotalRevenue();
    }
}
