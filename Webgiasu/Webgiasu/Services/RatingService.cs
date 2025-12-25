using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Webgiasu.Models;
using Webgiasu.Models.ViewModels;

namespace Webgiasu.Services
{
    public class RatingService : IRatingService
    {
        private readonly AppDbContext _db;
        public RatingService(AppDbContext db) => _db = db;

        public async Task<bool> AddRatingAsync(int problemId, int studentId, int tutorId, int stars, string? comment)
        {
            if (await _db.Ratings.AnyAsync(r => r.ProblemId == problemId && r.StudentId == studentId))
                return false;

            var rating = new Rating
            {
                ProblemId = problemId,
                StudentId = studentId,
                TutorId = tutorId,
                Stars = stars,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _db.Ratings.Add(rating);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasStudentRatedProblemAsync(int problemId, int studentId)
            => await _db.Ratings.AnyAsync(r => r.ProblemId == problemId && r.StudentId == studentId);

        public async Task<List<Rating>> GetRatingsByTutorIdAsync(int tutorId)
            => await _db.Ratings.Where(r => r.TutorId == tutorId).OrderByDescending(r => r.CreatedAt).ToListAsync();

        public async Task<TutorRatingsSummaryViewModel> GetTutorRatingsSummaryAsync(int tutorId)
        {
            var tutor = await _db.Users.FindAsync(tutorId);
            var tutorRatings = await _db.Ratings.Where(r => r.TutorId == tutorId).ToListAsync();

            var starDistribution = new Dictionary<int, int>();
            for (int i = 1; i <= 5; i++) starDistribution[i] = tutorRatings.Count(r => r.Stars == i);

            var recentRatings = new List<RatingDetailViewModel>();
            foreach (var rating in tutorRatings.OrderByDescending(r => r.CreatedAt).Take(10))
            {
                var student = await _db.Users.FindAsync(rating.StudentId);
                var problem = await _db.Problems.FindAsync(rating.ProblemId);

                recentRatings.Add(new RatingDetailViewModel
                {
                    RatingId = rating.RatingId,
                    StudentName = student?.FullName ?? "Unknown",
                    ProblemTitle = problem?.Title ?? "Unknown",
                    Stars = rating.Stars,
                    Comment = rating.Comment,
                    CreatedAt = rating.CreatedAt
                });
            }

            return new TutorRatingsSummaryViewModel
            {
                TutorId = tutorId,
                TutorName = tutor?.FullName ?? "Unknown",
                AverageRating = tutorRatings.Any() ? tutorRatings.Average(r => r.Stars) : 0,
                TotalRatings = tutorRatings.Count,
                StarDistribution = starDistribution,
                RecentRatings = recentRatings
            };
        }

        public async Task<double> GetAverageRatingForTutorAsync(int tutorId)
        {
            var list = await _db.Ratings.Where(r => r.TutorId == tutorId).ToListAsync();
            return list.Any() ? list.Average(r => r.Stars) : 0;
        }

        public async Task<Rating?> GetRatingByIdAsync(int ratingId) => await _db.Ratings.FindAsync(ratingId);
    }
}
