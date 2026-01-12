using Microsoft.EntityFrameworkCore;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class SchoolClassService : ISchoolClassService
    {
        private readonly AppDbContext _db;

        public SchoolClassService(AppDbContext db)
        {
            _db = db;
        }

        public List<SchoolClass> GetClassesBySchoolId(int schoolId)
        {
            try
            {
                return _db.SchoolClasses
                    .Include(sc => sc.Tutor)
                    .Include(sc => sc.School)
                    .Include(sc => sc.Students)
                    .Where(c => c.SchoolId == schoolId)
                    .OrderByDescending(c => c.CreatedDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetClassesBySchoolId: {ex.Message}");
                return new List<SchoolClass>();
            }
        }

        public SchoolClass? GetClassById(int classId)
        {
            try
            {
                return _db.SchoolClasses
                    .Include(sc => sc.Tutor)
                    .Include(sc => sc.School)
                    .Include(sc => sc.Students)
                    .FirstOrDefault(c => c.Id == classId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetClassById: {ex.Message}");
                return null;
            }
        }

        public bool CreateClass(SchoolClass schoolClass, List<int> studentIds)
        {
            try
            {
                // Đảm bảo có CreatedDate
                if (schoolClass.CreatedDate == default)
                {
                    schoolClass.CreatedDate = DateTime.Now;
                }

                // Thêm class vào database
                _db.SchoolClasses.Add(schoolClass);
                _db.SaveChanges();

                // Thêm students vào class
                if (studentIds != null && studentIds.Any())
                {
                    foreach (var studentId in studentIds)
                    {
                        var classStudent = new ClassStudent
                        {
                            ClassId = schoolClass.Id,
                            StudentId = studentId,
                            JoinedDate = DateTime.Now
                        };
                        _db.ClassStudents.Add(classStudent);
                    }
                    _db.SaveChanges();
                }

                Console.WriteLine($"✅ Created class '{schoolClass.ClassName}' with ID {schoolClass.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateClass: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public bool UpdateClass(SchoolClass schoolClass)
        {
            try
            {
                var existingClass = _db.SchoolClasses.Find(schoolClass.Id);
                if (existingClass == null)
                {
                    Console.WriteLine($"❌ Class with ID {schoolClass.Id} not found");
                    return false;
                }

                existingClass.ClassName = schoolClass.ClassName;
                existingClass.Subject = schoolClass.Subject;
                existingClass.Description = schoolClass.Description;
                existingClass.TutorId = schoolClass.TutorId;
                existingClass.StartDate = schoolClass.StartDate;
                existingClass.EndDate = schoolClass.EndDate;
                existingClass.Status = schoolClass.Status;

                _db.SaveChanges();

                Console.WriteLine($"✅ Updated class '{existingClass.ClassName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in UpdateClass: {ex.Message}");
                return false;
            }
        }

        public bool DeleteClass(int classId)
        {
            try
            {
                var classToRemove = _db.SchoolClasses
                    .Include(sc => sc.Students)
                    .FirstOrDefault(c => c.Id == classId);

                if (classToRemove == null)
                {
                    Console.WriteLine($"❌ Class with ID {classId} not found");
                    return false;
                }

                // Xóa tất cả students trong class (cascade sẽ tự động xóa nếu đã config trong AppDbContext)
                _db.SchoolClasses.Remove(classToRemove);
                _db.SaveChanges();

                Console.WriteLine($"✅ Deleted class '{classToRemove.ClassName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in DeleteClass: {ex.Message}");
                return false;
            }
        }

        public List<int> GetClassStudentIds(int classId)
        {
            try
            {
                return _db.ClassStudents
                    .Where(cs => cs.ClassId == classId)
                    .Select(cs => cs.StudentId)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetClassStudentIds: {ex.Message}");
                return new List<int>();
            }
        }
    }
}