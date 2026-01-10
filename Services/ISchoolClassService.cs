using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface ISchoolClassService
    {
        List<SchoolClass> GetClassesBySchoolId(int schoolId);
        SchoolClass? GetClassById(int classId);
        bool CreateClass(SchoolClass schoolClass, List<int> studentIds);
        bool UpdateClass(SchoolClass schoolClass);
        bool DeleteClass(int classId);
        List<int> GetClassStudentIds(int classId);
    }
}
