using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface ITutorApplicationService
    {
        // Tutor actions
        bool ApplyForProblem(int problemId, int tutorId, string proposal, decimal proposedPrice, int estimatedDays);
        bool WithdrawApplication(int applicationId, int tutorId);
        List<TutorApplication> GetTutorApplications(int tutorId);

        // Student actions
        List<TutorApplication> GetApplicationsForProblem(int problemId, bool prioritizePremium = true);
        bool ApproveApplication(int applicationId, int studentId);
        bool RejectApplication(int applicationId, int studentId);

        // Common
        TutorApplication? GetApplicationById(int id);
        bool HasTutorApplied(int problemId, int tutorId);
    }
}