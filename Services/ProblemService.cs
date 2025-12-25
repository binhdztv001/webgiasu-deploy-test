using System;
using System.Collections.Generic;
using System.Linq;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class ProblemService : IProblemService
    {
        private readonly AppDbContext _db;
        public ProblemService(AppDbContext db) => _db = db;

        public List<Problem> GetAllProblems() => _db.Problems.OrderByDescending(p => p.CreatedDate).ToList();

        public List<Problem> GetProblemsByStudentId(int studentId) =>
            _db.Problems.Where(p => p.StudentId == studentId).OrderByDescending(p => p.CreatedDate).ToList();

        public List<Problem> GetAvailableProblems() =>
            _db.Problems.Where(p => p.Status == ProblemStatus.WaitingForTutor).OrderByDescending(p => p.CreatedDate).ToList();

        public List<Problem> GetProblemsByTutorId(int tutorId) =>
            _db.Problems.Where(p => p.AssignedTutorId == tutorId).OrderByDescending(p => p.CreatedDate).ToList();

        public Problem? GetProblemById(int id) => _db.Problems.Find(id);

        public bool CreateProblem(Problem problem)
        {
            problem.CreatedDate = DateTime.Now;
            problem.Status = ProblemStatus.WaitingForTutor;
            _db.Problems.Add(problem);
            _db.SaveChanges();
            return true;
        }

        public bool UpdateProblem(Problem problem)
        {
            var existing = _db.Problems.Find(problem.Id);
            if (existing == null) return false;

            existing.Title = problem.Title;
            existing.Description = problem.Description;
            existing.Type = problem.Type;
            existing.Difficulty = problem.Difficulty;
            existing.Deadline = problem.Deadline;
            existing.Status = problem.Status;
            existing.AssignedTutorId = problem.AssignedTutorId;
            existing.Price = problem.Price;

            _db.SaveChanges();
            return true;
        }

        public bool AssignProblemToTutor(int problemId, int tutorId)
        {
            var p = _db.Problems.Find(problemId);
            if (p == null || p.Status != ProblemStatus.WaitingForTutor) return false;
            p.AssignedTutorId = tutorId;
            p.Status = ProblemStatus.InProgress;
            _db.SaveChanges();
            return true;
        }

        public bool DeleteProblem(int id)
        {
            var p = _db.Problems.Find(id);
            if (p == null) return false;
            _db.Problems.Remove(p);
            _db.SaveChanges();
            return true;
        }
    }
}
