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

        // Thanh toán Problem nhóm (mới)
        GroupPayment? CreateGroupPayment(int groupId, int memberId, int userId, decimal amount);
        GroupPayment? GetGroupPaymentById(int id);
        GroupPayment? GetGroupPaymentByMemberId(int memberId);
        List<GroupPayment> GetGroupPaymentsByGroupId(int groupId);
        List<GroupPayment> GetGroupPaymentsByUserId(int userId);
        bool UpdateGroupPaymentStatus(int groupPaymentId, PaymentStatus status);
    
    }
}
