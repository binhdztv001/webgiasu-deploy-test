using System.Collections.Generic;
using System.Threading.Tasks;
using Webgiasu.Models;
using Webgiasu.Models.ViewModels;

namespace Webgiasu.Services
{
    public interface IRatingService
    {
        Task<bool> AddRatingAsync(int problemId, int studentId, int tutorId, int stars, string? comment);
        Task<bool> HasStudentRatedProblemAsync(int problemId, int studentId);
        Task<List<Rating>> GetRatingsByTutorIdAsync(int tutorId);
        Task<TutorRatingsSummaryViewModel> GetTutorRatingsSummaryAsync(int tutorId);
        Task<double> GetAverageRatingForTutorAsync(int tutorId);
        Task<Rating?> GetRatingByIdAsync(int ratingId);
    }
}
