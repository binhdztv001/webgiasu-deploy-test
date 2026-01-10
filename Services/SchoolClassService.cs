using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class SchoolClassService : ISchoolClassService
    {
        private static List<SchoolClass> _classes = new List<SchoolClass>();
        private static List<ClassStudent> _classStudents = new List<ClassStudent>();
        private static int _nextClassId = 1;
        private static int _nextClassStudentId = 1;

        public SchoolClassService()
        {
            // Initialize with some demo data if needed
            if (_classes.Count == 0)
            {
                InitializeDemoData();
            }
        }

        private void InitializeDemoData()
        {
            // Add demo class
            var demoClass = new SchoolClass
            {
                Id = _nextClassId++,
                SchoolId = 1, // Assuming school with ID 1
                ClassName = "Lớp Toán 10A",
                Subject = "Toán",
                Description = "Lớp học Toán nâng cao cho học sinh lớp 10",
                TutorId = 1, // Tutor1
                StartDate = DateTime.Now.AddDays(-7),
                EndDate = DateTime.Now.AddMonths(3),
                CreatedDate = DateTime.Now.AddDays(-10),
                Status = ClassStatus.Active
            };
            _classes.Add(demoClass);

            // Add demo students
            _classStudents.Add(new ClassStudent
            {
                Id = _nextClassStudentId++,
                ClassId = demoClass.Id,
                StudentId = 1, // Student1
                JoinedDate = DateTime.Now.AddDays(-7)
            });

            _classStudents.Add(new ClassStudent
            {
                Id = _nextClassStudentId++,
                ClassId = demoClass.Id,
                StudentId = 2, // Student2
                JoinedDate = DateTime.Now.AddDays(-7)
            });
        }

        public List<SchoolClass> GetClassesBySchoolId(int schoolId)
        {
            return _classes.Where(c => c.SchoolId == schoolId).ToList();
        }

        public SchoolClass? GetClassById(int classId)
        {
            return _classes.FirstOrDefault(c => c.Id == classId);
        }

        public bool CreateClass(SchoolClass schoolClass, List<int> studentIds)
        {
            try
            {
                schoolClass.Id = _nextClassId++;
                schoolClass.CreatedDate = DateTime.Now;
                schoolClass.Status = ClassStatus.Active;

                _classes.Add(schoolClass);

                // Add students to class
                if (studentIds != null && studentIds.Any())
                {
                    foreach (var studentId in studentIds)
                    {
                        _classStudents.Add(new ClassStudent
                        {
                            Id = _nextClassStudentId++,
                            ClassId = schoolClass.Id,
                            StudentId = studentId,
                            JoinedDate = DateTime.Now
                        });
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateClass(SchoolClass schoolClass)
        {
            var existingClass = _classes.FirstOrDefault(c => c.Id == schoolClass.Id);
            if (existingClass == null) return false;

            existingClass.ClassName = schoolClass.ClassName;
            existingClass.Subject = schoolClass.Subject;
            existingClass.Description = schoolClass.Description;
            existingClass.TutorId = schoolClass.TutorId;
            existingClass.StartDate = schoolClass.StartDate;
            existingClass.EndDate = schoolClass.EndDate;
            existingClass.Status = schoolClass.Status;

            return true;
        }

        public bool DeleteClass(int classId)
        {
            var classToRemove = _classes.FirstOrDefault(c => c.Id == classId);
            if (classToRemove == null) return false;

            _classes.Remove(classToRemove);

            // Remove all students from this class
            _classStudents.RemoveAll(cs => cs.ClassId == classId);

            return true;
        }

        public List<int> GetClassStudentIds(int classId)
        {
            return _classStudents
                .Where(cs => cs.ClassId == classId)
                .Select(cs => cs.StudentId)
                .ToList();
        }
    }
}
