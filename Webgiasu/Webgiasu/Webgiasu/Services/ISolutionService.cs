using System.Collections.Generic;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface ISolutionService
    {
        List<Solution> GetAllSolutions();
        Solution? GetSolutionById(int id);
        Solution? GetSolutionByProblemId(int problemId);
        List<Solution> GetSolutionsByTutorId(int tutorId);
        bool CreateSolution(Solution solution);
        bool UpdateSolution(Solution solution);
    }
}
