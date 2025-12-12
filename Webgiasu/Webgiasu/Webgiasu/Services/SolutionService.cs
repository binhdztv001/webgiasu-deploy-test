using System;
using System.Collections.Generic;
using System.Linq;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class SolutionService : ISolutionService
    {
        private readonly AppDbContext _db;
        public SolutionService(AppDbContext db) => _db = db;

        public List<Solution> GetAllSolutions() => _db.Solutions.OrderByDescending(s => s.SubmittedDate).ToList();

        public Solution? GetSolutionById(int id) => _db.Solutions.Find(id);

        public Solution? GetSolutionByProblemId(int problemId) => _db.Solutions.FirstOrDefault(s => s.ProblemId == problemId);

        public List<Solution> GetSolutionsByTutorId(int tutorId) =>
            _db.Solutions.Where(s => s.TutorId == tutorId).OrderByDescending(s => s.SubmittedDate).ToList();

        public bool CreateSolution(Solution solution)
        {
            solution.SubmittedDate = DateTime.Now;
            _db.Solutions.Add(solution);
            _db.SaveChanges();
            return true;
        }

        public bool UpdateSolution(Solution solution)
        {
            var existing = _db.Solutions.Find(solution.Id);
            if (existing == null) return false;

            existing.Content = solution.Content;
            existing.FileUrl = solution.FileUrl;
            existing.Rating = solution.Rating;
            existing.Feedback = solution.Feedback;

            _db.SaveChanges();
            return true;
        }
    }
}
