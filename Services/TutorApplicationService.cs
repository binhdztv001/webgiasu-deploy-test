using Microsoft.EntityFrameworkCore;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class TutorApplicationService : ITutorApplicationService
    {
        private readonly AppDbContext _db;
        private readonly IProblemService _problemService;

        public TutorApplicationService(AppDbContext db, IProblemService problemService)
        {
            _db = db;
            _problemService = problemService;
        }

        public bool ApplyForProblem(int problemId, int tutorId, string proposal, decimal proposedPrice, int estimatedDays)
        {
            try
            {
                var problem = _problemService.GetProblemById(problemId);
                if (problem == null || problem.Status != ProblemStatus.WaitingForTutor)
                    return false;

                // ✅ Kiểm tra đã apply chưa
                if (HasTutorApplied(problemId, tutorId))
                    return false;

                var application = new TutorApplication
                {
                    ProblemId = problemId,
                    TutorId = tutorId,
                    Proposal = proposal,
                    ProposedPrice = proposedPrice,
                    EstimatedDays = estimatedDays,
                    Status = ApplicationStatus.Pending,
                    AppliedDate = DateTime.Now
                };

                _db.TutorApplications.Add(application);
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error ApplyForProblem: {ex.Message}");
                return false;
            }
        }

        public List<TutorApplication> GetApplicationsForProblem(int problemId, bool prioritizePremium = true)
        {
            var query = _db.TutorApplications
                .Include(ta => ta.Tutor)
                .Include(ta => ta.Problem)
                .Where(ta => ta.ProblemId == problemId && ta.Status == ApplicationStatus.Pending);

            if (prioritizePremium)
            {
                // ✅ ƯU TIÊN PREMIUM TUTORS
                return query.OrderByDescending(ta => ta.Tutor!.IsPremium)
                           .ThenByDescending(ta => ta.AppliedDate)
                           .ToList();
            }

            return query.OrderByDescending(ta => ta.AppliedDate).ToList();
        }

        public bool ApproveApplication(int applicationId, int studentId)
        {
            try
            {
                Console.WriteLine($"📝 ApproveApplication called: appId={applicationId}, studentId={studentId}");

                // ✅ FIX: Eager load đầy đủ
                var application = _db.TutorApplications
                    .Include(ta => ta.Problem)
                    .Include(ta => ta.Tutor)
                    .FirstOrDefault(ta => ta.Id == applicationId);

                // ✅ LOG: Debug info
                if (application == null)
                {
                    Console.WriteLine($"❌ Application not found: {applicationId}");
                    return false;
                }

                Console.WriteLine($"✅ Application found: Status={application.Status}");

                if (application.Problem == null)
                {
                    Console.WriteLine($"❌ Problem is null for application {applicationId}");
                    return false;
                }

                Console.WriteLine($"✅ Problem found: Id={application.Problem.Id}, StudentId={application.Problem.StudentId}, Status={application.Problem.Status}");

                // ✅ Kiểm tra ownership
                if (application.Problem.StudentId != studentId)
                {
                    Console.WriteLine($"❌ StudentId mismatch: expected={studentId}, actual={application.Problem.StudentId}");
                    return false;
                }

                // ✅ Kiểm tra status
                if (application.Status != ApplicationStatus.Pending)
                {
                    Console.WriteLine($"❌ Application status is not Pending: {application.Status}");
                    return false;
                }

                // ✅ Kiểm tra problem status
                if (application.Problem.Status != ProblemStatus.WaitingForTutor)
                {
                    Console.WriteLine($"❌ Problem status is not WaitingForTutor: {application.Problem.Status}");
                    return false;
                }

                Console.WriteLine($"✅ All checks passed. Approving application...");

                // ✅ Cập nhật application
                application.Status = ApplicationStatus.Approved;
                application.ResponsedDate = DateTime.Now;

                // ✅ Gán Tutor cho Problem
                var problem = application.Problem;
                problem.AssignedTutorId = application.TutorId;
                problem.Status = ProblemStatus.InProgress;

                Console.WriteLine($"✅ Updated: Application.Status={application.Status}, Problem.Status={problem.Status}, AssignedTutorId={problem.AssignedTutorId}");

                // ✅ Từ chối các application khác
                var otherApplications = _db.TutorApplications
                    .Where(ta => ta.ProblemId == application.ProblemId
                             && ta.Id != applicationId
                             && ta.Status == ApplicationStatus.Pending)
                    .ToList();

                Console.WriteLine($"✅ Found {otherApplications.Count} other applications to reject");

                foreach (var other in otherApplications)
                {
                    other.Status = ApplicationStatus.Rejected;
                    other.ResponsedDate = DateTime.Now;
                }

                _db.SaveChanges();
                Console.WriteLine($"✅ SaveChanges completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error ApproveApplication: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public bool RejectApplication(int applicationId, int studentId)
        {
            try
            {
                var application = _db.TutorApplications
                    .Include(ta => ta.Problem)
                    .FirstOrDefault(ta => ta.Id == applicationId);

                if (application == null || application.Problem?.StudentId != studentId)
                    return false;

                application.Status = ApplicationStatus.Rejected;
                application.ResponsedDate = DateTime.Now;

                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error RejectApplication: {ex.Message}");
                return false;
            }
        }

        public bool WithdrawApplication(int applicationId, int tutorId)
        {
            try
            {
                var application = _db.TutorApplications
                    .FirstOrDefault(ta => ta.Id == applicationId && ta.TutorId == tutorId);

                if (application == null || application.Status != ApplicationStatus.Pending)
                    return false;

                application.Status = ApplicationStatus.Withdrawn;
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error WithdrawApplication: {ex.Message}");
                return false;
            }
        }

        public List<TutorApplication> GetTutorApplications(int tutorId)
        {
            return _db.TutorApplications
                .Include(ta => ta.Problem)
                .Where(ta => ta.TutorId == tutorId)
                .OrderByDescending(ta => ta.AppliedDate)
                .ToList();
        }

        public TutorApplication? GetApplicationById(int id)
        {
            return _db.TutorApplications
                .Include(ta => ta.Problem)
                .Include(ta => ta.Tutor)
                .FirstOrDefault(ta => ta.Id == id);
        }

        public bool HasTutorApplied(int problemId, int tutorId)
        {
            return _db.TutorApplications
                .Any(ta => ta.ProblemId == problemId
                       && ta.TutorId == tutorId
                       && ta.Status == ApplicationStatus.Pending);
        }
    }
}