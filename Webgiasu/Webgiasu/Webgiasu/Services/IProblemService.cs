using System.Collections.Generic;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface IProblemService
    {
        List<Problem> GetAllProblems();
        List<Problem> GetProblemsByStudentId(int studentId);
        List<Problem> GetAvailableProblems();
        List<Problem> GetProblemsByTutorId(int tutorId);
        Problem? GetProblemById(int id);
        bool CreateProblem(Problem problem);
        bool UpdateProblem(Problem problem);
        bool AssignProblemToTutor(int problemId, int tutorId);
        bool DeleteProblem(int id);
    }
}
