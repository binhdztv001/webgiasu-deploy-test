using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models;
using Webgiasu.Services;

namespace Webgiasu.Controllers
{
    public class SchoolController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IUserService _userService;
        private readonly ISchoolClassService _classService;

        public SchoolController(AppDbContext db, IUserService userService, ISchoolClassService classService)
        {
            _db = db;
            _userService = userService;
            _classService = classService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        public IActionResult Dashboard()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var school = _userService.GetUserById(userId);
                if (school == null || school.Role != UserRole.School)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Thống kê tổng quan
                var classes = _classService.GetClassesBySchoolId(userId);
                ViewBag.TotalClasses = classes.Count;
                ViewBag.ActiveClasses = classes.Count(c => c.Status == ClassStatus.Active);
                
                // Count students and tutors
                var tutorIds = classes.Where(c => c.TutorId.HasValue).Select(c => c.TutorId.Value).Distinct().ToList();
                ViewBag.TotalTutors = tutorIds.Count;
                
                // For students, we'd need to count from ClassStudent table
                ViewBag.TotalStudents = 0; // TODO: Count from _classService.GetClassStudents

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in School Dashboard: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang chủ!";
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult ManageClasses()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Load classes from service
                var classes = _classService.GetClassesBySchoolId(userId);
                
                // Create view model with tutor and student info
                var classViewModels = classes.Select(c => new Models.ViewModels.ClassViewModel
                {
                    Id = c.Id,
                    ClassName = c.ClassName,
                    Subject = c.Subject,
                    Description = c.Description ?? "",
                    TutorName = c.TutorId.HasValue ? _userService.GetUserById(c.TutorId.Value)?.FullName ?? "Chưa có" : "Chưa có",
                    StudentCount = _classService.GetClassStudentIds(c.Id).Count,
                    StartDate = c.StartDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                    Status = c.Status
                }).ToList();

                return View(classViewModels);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ManageClasses: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        // Edit Classes - List all classes for editing
        public IActionResult EditClasses()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Load classes from service
                var classes = _classService.GetClassesBySchoolId(userId);
                
                // Create view model
                var classViewModels = classes.Select(c => new Models.ViewModels.ClassViewModel
                {
                    Id = c.Id,
                    ClassName = c.ClassName,
                    Subject = c.Subject,
                    Description = c.Description ?? "",
                    TutorName = c.TutorId.HasValue ? _userService.GetUserById(c.TutorId.Value)?.FullName ?? "Chưa có" : "Chưa có",
                    StudentCount = _classService.GetClassStudentIds(c.Id).Count,
                    StartDate = c.StartDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                    Status = c.Status
                }).ToList();

                return View(classViewModels);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in EditClasses: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult CreateClass()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // ✅ CHỈ LẤY HỌC SINH VÀ MENTOR CẤP ĐẠI HỌC
                var allStudents = _userService.GetUsersByRole(UserRole.Student);
                var allTutors = _userService.GetUsersByRole(UserRole.Tutor);

                // Lọc chỉ lấy những người cấp Đại học
                var universityStudents = allStudents
                    .Where(s => s.Level.HasValue && s.Level.Value == EducationLevel.DaiHoc)
                    .ToList();

                var universityTutors = allTutors
                    .Where(t => t.Level.HasValue && t.Level.Value == EducationLevel.DaiHoc && t.IsApproved)
                    .ToList();

                ViewBag.Students = universityStudents;
                ViewBag.Tutors = universityTutors;

                // Thông báo nếu không có mentor/học sinh phù hợp
                if (!universityTutors.Any())
                {
                    TempData["Warning"] = "Hiện không có Mentor cấp Đại học trong hệ thống!";
                }
                if (!universityStudents.Any())
                {
                    TempData["Warning"] = "Hiện không có Học sinh cấp Đại học trong hệ thống!";
                }

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateClass: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult CreateClass(string className, string subject, string description, int? tutorId, List<int> studentIds, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Validate cơ bản
                if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(subject))
                {
                    TempData["Error"] = "Vui lòng nhập đầy đủ thông tin!";
                    return RedirectToAction("CreateClass");
                }

                if (!tutorId.HasValue || tutorId.Value == 0)
                {
                    TempData["Error"] = "Vui lòng chọn Mentor!";
                    return RedirectToAction("CreateClass");
                }

                if (studentIds == null || !studentIds.Any())
                {
                    TempData["Error"] = "Vui lòng chọn ít nhất 1 học sinh!";
                    return RedirectToAction("CreateClass");
                }

                // ✅ KIỂM TRA MENTOR CẤP ĐẠI HỌC
                var selectedTutor = _userService.GetUserById(tutorId.Value);
                if (selectedTutor == null || selectedTutor.Role != UserRole.Tutor)
                {
                    TempData["Error"] = "Mentor không hợp lệ!";
                    return RedirectToAction("CreateClass");
                }

                if (!selectedTutor.Level.HasValue || selectedTutor.Level.Value != EducationLevel.DaiHoc)
                {
                    TempData["Error"] = "Chỉ Mentor cấp Đại học mới được phép giảng dạy!";
                    return RedirectToAction("CreateClass");
                }

                if (!selectedTutor.IsApproved)
                {
                    TempData["Error"] = "Mentor chưa được phê duyệt!";
                    return RedirectToAction("CreateClass");
                }

                // ✅ KIỂM TRA TẤT CẢ HỌC SINH ĐỀU CẤP ĐẠI HỌC
                var selectedStudents = _userService.GetUsersByRole(UserRole.Student)
                    .Where(s => studentIds.Contains(s.Id))
                    .ToList();

                if (selectedStudents.Count != studentIds.Count)
                {
                    TempData["Error"] = "Một số học sinh không hợp lệ!";
                    return RedirectToAction("CreateClass");
                }

                var nonUniversityStudents = selectedStudents
                    .Where(s => !s.Level.HasValue || s.Level.Value != EducationLevel.DaiHoc)
                    .ToList();

                if (nonUniversityStudents.Any())
                {
                    var studentNames = string.Join(", ", nonUniversityStudents.Select(s => s.FullName));
                    TempData["Error"] = $"Chỉ học sinh cấp Đại học mới được phép tham gia! Học sinh không đủ điều kiện: {studentNames}";
                    return RedirectToAction("CreateClass");
                }

                // ✅ TẤT CẢ ĐIỀU KIỆN ĐÃ ĐẠT - TẠO LỚP HỌC
                var newClass = new SchoolClass
                {
                    SchoolId = userId,
                    ClassName = className,
                    Subject = subject,
                    Description = description,
                    TutorId = tutorId.Value,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = ClassStatus.Active,
                    CreatedDate = DateTime.Now
                };

                if (_classService.CreateClass(newClass, studentIds))
                {
                    TempData["Success"] = "Tạo lớp học thành công với Mentor và Học sinh cấp Đại học!";
                    return RedirectToAction("ManageClasses");
                }
                else
                {
                    TempData["Error"] = "Không thể tạo lớp học!";
                    return RedirectToAction("CreateClass");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateClass POST: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo lớp học: {ex.Message}";
                return RedirectToAction("CreateClass");
            }
        }

        public IActionResult Statistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Thống kê chi tiết
                var classes = _classService.GetClassesBySchoolId(userId);
                
                ViewBag.TotalClasses = classes.Count;
                ViewBag.ActiveClasses = classes.Count(c => c.Status == ClassStatus.Active);
                ViewBag.CompletedClasses = classes.Count(c => c.Status == ClassStatus.Completed);
                ViewBag.OngoingClasses = classes.Count(c => c.Status == ClassStatus.Active);
                
                var tutorIds = classes.Where(c => c.TutorId.HasValue).Select(c => c.TutorId.Value).Distinct().ToList();
                ViewBag.TotalTutors = tutorIds.Count;
                ViewBag.TotalStudents = 0; // TODO

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Statistics: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult Settings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var school = _userService.GetUserById(userId);
                return View(school);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Settings: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult UpdateProfile(string fullName, string email, string phoneNumber)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var school = _userService.GetUserById(userId);
                if (school != null)
                {
                    school.FullName = fullName;
                    school.Email = email;
                    school.PhoneNumber = phoneNumber;

                    _userService.UpdateUser(school);
                    HttpContext.Session.SetString("UserName", school.FullName);

                    TempData["Success"] = "Cập nhật thông tin thành công!";
                }
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in UpdateProfile: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Settings");
            }
        }

        // Class Details
        public IActionResult ClassDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var classInfo = _classService.GetClassById(id);
                if (classInfo == null || classInfo.SchoolId != userId)
                {
                    TempData["Error"] = "Không tìm thấy lớp học!";
                    return RedirectToAction("ManageClasses");
                }

                // Get student IDs and fetch user details
                var studentIds = _classService.GetClassStudentIds(id);
                var students = studentIds.Select(sid =>
                {
                    var student = _userService.GetUserById(sid);
                    return student != null ? new Models.ViewModels.StudentInClassViewModel
                    {
                        Id = student.Id,
                        FullName = student.FullName,
                        Email = student.Email ?? "",
                        PhoneNumber = student.PhoneNumber ?? "",
                        JoinedDate = DateTime.Now.ToString("dd/MM/yyyy") // TODO: Get from ClassStudent
                    } : null;
                }).Where(s => s != null).Cast<Models.ViewModels.StudentInClassViewModel>().ToList();

                // Get tutor info
                var tutor = classInfo.TutorId.HasValue ? _userService.GetUserById(classInfo.TutorId.Value) : null;

                // Calculate duration
                var duration = "Chưa xác định";
                if (classInfo.StartDate.HasValue && classInfo.EndDate.HasValue)
                {
                    var days = (classInfo.EndDate.Value - classInfo.StartDate.Value).Days;
                    duration = $"{days} ngày";
                }

                var viewModel = new Models.ViewModels.ClassDetailsViewModel
                {
                    Id = classInfo.Id,
                    ClassName = classInfo.ClassName,
                    Subject = classInfo.Subject,
                    Description = classInfo.Description ?? "",
                    TutorName = tutor?.FullName ?? "Chưa có",
                    TutorEmail = tutor?.Email ?? "",
                    TutorPhone = tutor?.PhoneNumber ?? "",
                    StartDate = classInfo.StartDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                    EndDate = classInfo.EndDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                    CreatedDate = classInfo.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                    Status = classInfo.Status,
                    Students = students,
                    Duration = duration
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ClassDetails: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("ManageClasses");
            }
        }

        // Edit Class - GET
        public IActionResult EditClass(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var classInfo = _classService.GetClassById(id);
                if (classInfo == null || classInfo.SchoolId != userId)
                {
                    TempData["Error"] = "Không tìm thấy lớp học!";
                    return RedirectToAction("ManageClasses");
                }

                // ✅ CHỈ LẤY MENTOR CẤP ĐẠI HỌC ĐÃ ĐƯỢC DUYỆT
                var allTutors = _userService.GetUsersByRole(UserRole.Tutor);
                var universityTutors = allTutors
                    .Where(t => t.Level.HasValue && t.Level.Value == EducationLevel.DaiHoc && t.IsApproved)
                    .ToList();

                ViewBag.Tutors = universityTutors;

                var viewModel = new Models.ViewModels.EditClassViewModel
                {
                    Id = classInfo.Id,
                    ClassName = classInfo.ClassName,
                    Subject = classInfo.Subject,
                    Description = classInfo.Description ?? "",
                    TutorId = classInfo.TutorId,
                    StartDateValue = classInfo.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDateValue = classInfo.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Status = classInfo.Status,
                    CreatedDate = classInfo.CreatedDate.ToString("dd/MM/yyyy"),
                    StudentCount = _classService.GetClassStudentIds(id).Count
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in EditClass GET: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("ManageClasses");
            }
        }

        // Edit Class - POST
        [HttpPost]
        public IActionResult EditClass(int classId, string className, string subject, string description, int tutorId, DateTime? startDate, DateTime? endDate, int status)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var classInfo = _classService.GetClassById(classId);
                if (classInfo == null || classInfo.SchoolId != userId)
                {
                    TempData["Error"] = "Không tìm thấy lớp học!";
                    return RedirectToAction("ManageClasses");
                }

                // Validate
                if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(subject))
                {
                    TempData["Error"] = "Vui lòng nhập đầy đủ thông tin!";
                    return RedirectToAction("EditClass", new { id = classId });
                }

                if (tutorId == 0)
                {
                    TempData["Error"] = "Vui lòng chọn Mentor!";
                    return RedirectToAction("EditClass", new { id = classId });
                }

                // ✅ KIỂM TRA MENTOR CẤP ĐẠI HỌC
                var selectedTutor = _userService.GetUserById(tutorId);
                if (selectedTutor == null || selectedTutor.Role != UserRole.Tutor)
                {
                    TempData["Error"] = "Mentor không hợp lệ!";
                    return RedirectToAction("EditClass", new { id = classId });
                }

                if (!selectedTutor.Level.HasValue || selectedTutor.Level.Value != EducationLevel.DaiHoc)
                {
                    TempData["Error"] = "Chỉ Mentor cấp Đại học mới được phép giảng dạy!";
                    return RedirectToAction("EditClass", new { id = classId });
                }

                if (!selectedTutor.IsApproved)
                {
                    TempData["Error"] = "Mentor chưa được phê duyệt!";
                    return RedirectToAction("EditClass", new { id = classId });
                }

                // Update class
                classInfo.ClassName = className;
                classInfo.Subject = subject;
                classInfo.Description = description;
                classInfo.TutorId = tutorId;
                classInfo.StartDate = startDate;
                classInfo.EndDate = endDate;
                classInfo.Status = (ClassStatus)status;

                if (_classService.UpdateClass(classInfo))
                {
                    TempData["Success"] = "Cập nhật lớp học thành công!";
                    return RedirectToAction("ClassDetails", new { id = classId });
                }
                else
                {
                    TempData["Error"] = "Không thể cập nhật lớp học!";
                    return RedirectToAction("EditClass", new { id = classId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in EditClass POST: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("EditClass", new { id = classId });
            }
        }

        // Delete Class
        [HttpPost]
        public IActionResult DeleteClass(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Chưa đăng nhập!" });
                }

                var classInfo = _classService.GetClassById(id);
                if (classInfo == null || classInfo.SchoolId != userId)
                {
                    return Json(new { success = false, message = "Không tìm thấy lớp học!" });
                }

                if (_classService.DeleteClass(id))
                {
                    return Json(new { success = true, message = "Xóa lớp học thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa lớp học!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in DeleteClass: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }
    }
}
