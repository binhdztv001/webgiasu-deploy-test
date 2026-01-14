using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models;
using Webgiasu.Services;
using Microsoft.EntityFrameworkCore;

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

        // ============================================================
        // SCHEDULE MANAGEMENT ACTIONS
        // ============================================================

        // Danh sách lịch trao đổi
        public IActionResult ManageSchedules()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var schedules = _db.ClassSchedules
                    .Where(s => s.Class!.SchoolId == userId)
                    .OrderByDescending(s => s.ScheduleDate)
                    .Select(s => new Models.ViewModels.ScheduleListViewModel
                    {
                        Id = s.Id,
                        Title = s.Title,
                        ScheduleDate = s.ScheduleDate,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        MeetingType = s.MeetingType,
                        Status = s.Status,
                        ClassName = s.Class!.ClassName,
                        Subject = s.Class.Subject,
                        TotalStudents = s.Class.Students!.Count
                    })
                    .ToList();

                return View(schedules);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ManageSchedules: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        // Tạo lịch trao đổi - GET
        public IActionResult CreateSchedule(int? classId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Lấy danh sách lớp học
                var classes = _classService.GetClassesBySchoolId(userId);
                ViewBag.Classes = classes;

                var model = new Models.ViewModels.CreateScheduleViewModel();
                if (classId.HasValue)
                {
                    model.ClassId = classId.Value;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateSchedule GET: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("ManageSchedules");
            }
        }

        // Tạo lịch trao đổi - POST
        [HttpPost]
        public IActionResult CreateSchedule(Models.ViewModels.CreateScheduleViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                if (!ModelState.IsValid)
                {
                    var classes = _classService.GetClassesBySchoolId(userId);
                    ViewBag.Classes = classes;
                    return View(model);
                }

                // Validate class belongs to school
                var classInfo = _classService.GetClassById(model.ClassId);
                if (classInfo == null || classInfo.SchoolId != userId)
                {
                    TempData["Error"] = "Lớp học không hợp lệ!";
                    return RedirectToAction("CreateSchedule");
                }

                // Validate time
                if (model.StartTime >= model.EndTime)
                {
                    TempData["Error"] = "Giờ kết thúc phải sau giờ bắt đầu!";
                    var classes = _classService.GetClassesBySchoolId(userId);
                    ViewBag.Classes = classes;
                    return View(model);
                }

                // Create schedule
                var schedule = new ClassSchedule
                {
                    ClassId = model.ClassId,
                    Title = model.Title,
                    ScheduleDate = model.ScheduleDate,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    MeetingType = model.MeetingType,
                    Location = model.Location,
                    Content = model.Content,
                    Notes = model.Notes,
                    Status = ScheduleStatus.Upcoming,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now
                };

                _db.ClassSchedules.Add(schedule);
                _db.SaveChanges();

                TempData["Success"] = "Tạo lịch trao đổi thành công!";
                return RedirectToAction("ScheduleDetails", new { id = schedule.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateSchedule POST: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("CreateSchedule");
            }
        }

        // Chi tiết lịch trao đổi
        public IActionResult ScheduleDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var schedule = _db.ClassSchedules
                    .Where(s => s.Id == id && s.Class!.SchoolId == userId)
                    .Select(s => new Models.ViewModels.ScheduleDetailsViewModel
                    {
                        Id = s.Id,
                        Title = s.Title,
                        ScheduleDate = s.ScheduleDate,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        MeetingType = s.MeetingType,
                        Location = s.Location,
                        Content = s.Content,
                        Notes = s.Notes,
                        Status = s.Status,
                        CreatedDate = s.CreatedDate,
                        CreatorName = s.Creator!.FullName,
                        ClassId = s.ClassId,
                        ClassName = s.Class!.ClassName,
                        Subject = s.Class.Subject,
                        TutorName = s.Class.Tutor != null ? s.Class.Tutor.FullName : null,
                        TutorEmail = s.Class.Tutor != null ? s.Class.Tutor.Email : null,
                        TotalStudents = s.Class.Students!.Count
                    })
                    .FirstOrDefault();

                if (schedule == null)
                {
                    TempData["Error"] = "Không tìm thấy lịch trao đổi!";
                    return RedirectToAction("ManageSchedules");
                }

                return View(schedule);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ScheduleDetails: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("ManageSchedules");
            }
        }

        // Chỉnh sửa lịch trao đổi - GET
        public IActionResult EditSchedule(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var schedule = _db.ClassSchedules
                    .Include(s => s.Class)
                    .Where(s => s.Id == id && s.Class!.SchoolId == userId)
                    .FirstOrDefault();

                if (schedule == null)
                {
                    TempData["Error"] = "Không tìm thấy lịch trao đổi!";
                    return RedirectToAction("ManageSchedules");
                }

                var model = new Models.ViewModels.EditScheduleViewModel
                {
                    Id = schedule.Id,
                    Title = schedule.Title,
                    ScheduleDate = schedule.ScheduleDate,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    MeetingType = schedule.MeetingType,
                    Location = schedule.Location,
                    Content = schedule.Content,
                    Notes = schedule.Notes,
                    Status = schedule.Status,
                    ClassId = schedule.ClassId,
                    ClassName = schedule.Class!.ClassName
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in EditSchedule GET: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang chỉnh sửa!";
                return RedirectToAction("ManageSchedules");
            }
        }

        // Chỉnh sửa lịch trao đổi - POST
        [HttpPost]
        public IActionResult EditSchedule(Models.ViewModels.EditScheduleViewModel model, string StartTime, string EndTime)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Parse TimeSpan from string inputs
                if (!string.IsNullOrEmpty(StartTime) && TimeSpan.TryParse(StartTime, out var parsedStartTime))
                {
                    model.StartTime = parsedStartTime;
                }

                if (!string.IsNullOrEmpty(EndTime) && TimeSpan.TryParse(EndTime, out var parsedEndTime))
                {
                    model.EndTime = parsedEndTime;
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var schedule = _db.ClassSchedules
                    .Include(s => s.Class)
                    .Where(s => s.Id == model.Id && s.Class!.SchoolId == userId)
                    .FirstOrDefault();

                if (schedule == null)
                {
                    TempData["Error"] = "Không tìm thấy lịch trao đổi!";
                    return RedirectToAction("ManageSchedules");
                }

                // Validate time
                if (model.StartTime >= model.EndTime)
                {
                    TempData["Error"] = "Giờ kết thúc phải sau giờ bắt đầu!";
                    model.ClassName = schedule.Class!.ClassName;
                    return View(model);
                }

                // Update
                schedule.Title = model.Title;
                schedule.ScheduleDate = model.ScheduleDate;
                schedule.StartTime = model.StartTime;
                schedule.EndTime = model.EndTime;
                schedule.MeetingType = model.MeetingType;
                schedule.Location = model.Location;
                schedule.Content = model.Content;
                schedule.Notes = model.Notes;
                schedule.Status = model.Status;
                schedule.UpdatedDate = DateTime.Now;

                _db.SaveChanges();

                TempData["Success"] = "Cập nhật lịch trao đổi thành công!";
                return RedirectToAction("ScheduleDetails", new { id = schedule.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in EditSchedule POST: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật lịch trao đổi!";
                return RedirectToAction("EditSchedule", new { id = model.Id });
            }
        }

        // Xóa lịch trao đổi
        [HttpPost]
        public IActionResult DeleteSchedule(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Chưa đăng nhập!" });
                }

                var schedule = _db.ClassSchedules
                    .Where(s => s.Id == id && s.Class!.SchoolId == userId)
                    .FirstOrDefault();

                if (schedule == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch trao đổi!" });
                }

                _db.ClassSchedules.Remove(schedule);
                _db.SaveChanges();

                return Json(new { success = true, message = "Xóa lịch trao đổi thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in DeleteSchedule: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // ============================================================
        // END SCHEDULE MANAGEMENT ACTIONS
        // ============================================================

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
                
                // Count total students
                var totalStudents = 0;
                foreach (var c in classes)
                {
                    totalStudents += _classService.GetClassStudentIds(c.Id).Count;
                }
                ViewBag.TotalStudents = totalStudents;

                // Lấy danh sách lớp học để hiển thị (tối đa 5 lớp gần nhất)
                var recentClasses = classes
                    .OrderByDescending(c => c.CreatedDate)
                    .Take(5)
                    .Select(c => new Models.ViewModels.ClassViewModel
                    {
                        Id = c.Id,
                        ClassName = c.ClassName,
                        Subject = c.Subject,
                        Description = c.Description ?? "",
                        TutorName = c.TutorId.HasValue ? _userService.GetUserById(c.TutorId.Value)?.FullName ?? "Chưa có" : "Chưa có",
                        StudentCount = _classService.GetClassStudentIds(c.Id).Count,
                        StartDate = c.StartDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                        Status = c.Status
                    })
                    .ToList();

                ViewBag.RecentClasses = recentClasses;

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
        public IActionResult CreateClass(string className, string subject, string description, int? tutorId, List<int> studentIds, DateTime? startDate, DateTime? endDate, string meetingType)
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
                    MeetingType = string.IsNullOrEmpty(meetingType) ? "Online" : meetingType,
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
                
                // Tổng quan
                ViewBag.TotalClasses = classes.Count;
                ViewBag.ActiveClasses = classes.Count(c => c.Status == ClassStatus.Active);
                ViewBag.CompletedClasses = classes.Count(c => c.Status == ClassStatus.Completed);
                ViewBag.OngoingClasses = classes.Count(c => c.Status == ClassStatus.Active);
                
                // Đếm số mentor và học sinh
                var tutorIds = classes.Where(c => c.TutorId.HasValue).Select(c => c.TutorId.Value).Distinct().ToList();
                ViewBag.TotalTutors = tutorIds.Count;
                
                var totalStudents = 0;
                foreach (var c in classes)
                {
                    totalStudents += _classService.GetClassStudentIds(c.Id).Count;
                }
                ViewBag.TotalStudents = totalStudents;

                // Thống kê theo môn học
                var classBySubject = classes.GroupBy(c => c.Subject)
                    .Select(g => new { Subject = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();
                ViewBag.ClassesBySubject = (IEnumerable<dynamic>)classBySubject;

                // Thống kê theo trạng thái
                var classByStatus = new List<dynamic>
                {
                    new { Status = "Đang hoạt động", Count = classes.Count(c => c.Status == ClassStatus.Active) },
                    new { Status = "Đã hoàn thành", Count = classes.Count(c => c.Status == ClassStatus.Completed) },
                    new { Status = "Đã hủy", Count = classes.Count(c => c.Status == ClassStatus.Cancelled) }
                };
                ViewBag.ClassesByStatus = (IEnumerable<dynamic>)classByStatus;

                // Thống kê theo thời gian (6 tháng gần nhất)
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var classesByMonth = classes
                    .Where(c => c.CreatedDate >= sixMonthsAgo)
                    .GroupBy(c => new { c.CreatedDate.Year, c.CreatedDate.Month })
                    .Select(g => new
                    {
                        Month = $"Tháng {g.Key.Month}/{g.Key.Year}",
                        Count = g.Count(),
                        Order = g.Key.Year * 12 + g.Key.Month
                    })
                    .OrderBy(x => x.Order)
                    .ToList();
                ViewBag.ClassesByMonth = (IEnumerable<dynamic>)classesByMonth;

                // Thống kê số học sinh theo tháng
                var studentsByMonth = new List<dynamic>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthDate = DateTime.Now.AddMonths(-i);
                    var monthClasses = classes.Where(c => 
                        c.CreatedDate.Year == monthDate.Year && 
                        c.CreatedDate.Month == monthDate.Month
                    ).ToList();
                    
                    var monthStudentCount = 0;
                    foreach (var c in monthClasses)
                    {
                        monthStudentCount += _classService.GetClassStudentIds(c.Id).Count;
                    }
                    
                    studentsByMonth.Add(new 
                    { 
                        Month = $"Tháng {monthDate.Month}/{monthDate.Year}",
                        Count = monthStudentCount 
                    });
                }
                ViewBag.StudentsByMonth = (IEnumerable<dynamic>)studentsByMonth;

                // Top 5 môn học phổ biến
                var topSubjects = classBySubject.Take(5).ToList();
                ViewBag.TopSubjects = (IEnumerable<dynamic>)topSubjects;

                // Thống kê mentor hoạt động
                var activeTutors = classes
                    .Where(c => c.TutorId.HasValue && c.Status == ClassStatus.Active)
                    .Select(c => c.TutorId.Value)
                    .Distinct()
                    .Count();
                ViewBag.ActiveTutors = activeTutors;

                // Tỷ lệ hoàn thành
                var completionRate = classes.Count > 0 
                    ? (double)classes.Count(c => c.Status == ClassStatus.Completed) / classes.Count * 100 
                    : 0;
                ViewBag.CompletionRate = completionRate;

                // Danh sách lớp học chi tiết (lấy tất cả)
                var classDetailsList = classes
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => new
                    {
                        Id = c.Id,
                        ClassName = c.ClassName,
                        Subject = c.Subject,
                        TutorName = c.TutorId.HasValue ? _userService.GetUserById(c.TutorId.Value)?.FullName ?? "Chưa có" : "Chưa có",
                        StudentCount = _classService.GetClassStudentIds(c.Id).Count,
                        StartDate = c.StartDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                        Status = c.Status,
                        StatusText = c.Status == ClassStatus.Active ? "Đang hoạt động" : 
                                   c.Status == ClassStatus.Completed ? "Đã hoàn thành" : "Đã hủy"
                    })
                    .ToList();
                ViewBag.ClassDetailsList = (IEnumerable<dynamic>)classDetailsList;

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

                // Get class schedules
                var schedules = _db.ClassSchedules
                    .Where(s => s.ClassId == id)
                    .OrderByDescending(s => s.ScheduleDate)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.ScheduleDate,
                        s.StartTime,
                        s.EndTime,
                        s.MeetingType,
                        s.Location,
                        s.Status
                    })
                    .ToList();

                ViewBag.Schedules = schedules;

                // ✅ TRUYỀN MEETING TYPE CỦA LỚP HỌC
                ViewBag.MeetingType = classInfo.MeetingType ?? "Online";

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

                // ✅ LẤY DANH SÁCH HỌC SINH CẤP ĐẠI HỌC
                var allStudents = _userService.GetUsersByRole(UserRole.Student);
                var universityStudents = allStudents
                    .Where(s => s.Level.HasValue && s.Level.Value == EducationLevel.DaiHoc)
                    .ToList();

                ViewBag.AllStudents = universityStudents;

                // ✅ LẤY DANH SÁCH HỌC SINH HIỆN TẠI CỦA LỚP
                var currentStudentIds = _classService.GetClassStudentIds(id);
                ViewBag.CurrentStudentIds = currentStudentIds;

                // ✅ TRUYỀN MEETING TYPE
                ViewBag.MeetingType = classInfo.MeetingType ?? "Online";

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
                    StudentCount = currentStudentIds.Count
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
        public IActionResult EditClass(int classId, string className, string subject, string description, int tutorId, List<int>? studentIds, DateTime? startDate, DateTime? endDate, int status, string meetingType)
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

                // ✅ KIỂM TRA DANH SÁCH HỌC SINH
                if (studentIds == null || !studentIds.Any())
                {
                    TempData["Error"] = "Vui lòng chọn ít nhất 1 học sinh!";
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

                // ✅ KIỂM TRA HỌC SINH CẤP ĐẠI HỌC
                var selectedStudents = _userService.GetUsersByRole(UserRole.Student)
                    .Where(s => studentIds.Contains(s.Id))
                    .ToList();

                if (selectedStudents.Count != studentIds.Count)
                {
                    TempData["Error"] = "Một số học sinh không hợp lệ!";
                    return RedirectToAction("EditClass", new { id = classId });
                }

                var nonUniversityStudents = selectedStudents
                    .Where(s => !s.Level.HasValue || s.Level.Value != EducationLevel.DaiHoc)
                    .ToList();

                if (nonUniversityStudents.Any())
                {
                    var studentNames = string.Join(", ", nonUniversityStudents.Select(s => s.FullName));
                    TempData["Error"] = $"Chỉ học sinh cấp Đại học mới được phép tham gia! Học sinh không đủ điều kiện: {studentNames}";
                    return RedirectToAction("EditClass", new { id = classId });
                }

                // Update class
                classInfo.ClassName = className;
                classInfo.Subject = subject;
                classInfo.Description = description;
                classInfo.TutorId = tutorId;
                classInfo.StartDate = startDate;
                classInfo.EndDate = endDate;
                classInfo.MeetingType = string.IsNullOrEmpty(meetingType) ? "Online" : meetingType;
                classInfo.Status = (ClassStatus)status;

                if (_classService.UpdateClass(classInfo))
                {
                    // ✅ CẬP NHẬT DANH SÁCH HỌC SINH
                    // Xóa tất cả học sinh hiện tại
                    var currentStudents = _db.ClassStudents.Where(cs => cs.ClassId == classId).ToList();
                    _db.ClassStudents.RemoveRange(currentStudents);

                    // Thêm danh sách học sinh mới
                    foreach (var studentId in studentIds)
                    {
                        _db.ClassStudents.Add(new ClassStudent
                        {
                            ClassId = classId,
                            StudentId = studentId,
                            JoinedDate = DateTime.Now
                        });
                    }

                    _db.SaveChanges();

                    TempData["Success"] = "Cập nhật lớp học và danh sách học sinh thành công!";
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
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật lớp học!";
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

        // ============================================================
        // MENTOR MANAGEMENT ACTIONS
        // ============================================================

        // Quản lý danh sách Mentor đã tạo
        public IActionResult ManageMentors()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Lấy tất cả mentor cấp Đại học đã được phê duyệt
                var mentors = _userService.GetUsersByRole(UserRole.Tutor)
                    .Where(m => m.Level.HasValue && m.Level.Value == EducationLevel.DaiHoc && m.IsApproved)
                    .OrderByDescending(m => m.RegisteredDate)
                    .ToList();

                return View(mentors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ManageMentors: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách Mentor!";
                return RedirectToAction("Dashboard");
            }
        }

        // Tạo tài khoản Mentor - GET
        public IActionResult CreateMentor()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateMentor GET: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        // Tạo tài khoản Mentor - POST
        [HttpPost]
        public IActionResult CreateMentor(string username, string password, string confirmPassword, 
            string fullName, string email, string phoneNumber, string bio, string subjects, 
            string education, int experienceYears, string certificates)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Validate input
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin bắt buộc!";
                    return RedirectToAction("CreateMentor");
                }

                // Check password match
                if (password != confirmPassword)
                {
                    TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                    return RedirectToAction("CreateMentor");
                }

                // Check username length
                if (username.Length < 3)
                {
                    TempData["Error"] = "Tên đăng nhập phải có ít nhất 3 ký tự!";
                    return RedirectToAction("CreateMentor");
                }

                // Check password length
                if (password.Length < 6)
                {
                    TempData["Error"] = "Mật khẩu phải có ít nhất 6 ký tự!";
                    return RedirectToAction("CreateMentor");
                }

                // Check if username already exists
                var existingUser = _userService.GetAllUsers()
                    .FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
                if (existingUser != null)
                {
                    TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                    return RedirectToAction("CreateMentor");
                }

                // Create new mentor account
                var mentor = new User
                {
                    Username = username,
                    Password = password,
                    FullName = fullName,
                    Email = email,
                    PhoneNumber = phoneNumber ?? "",
                    Role = UserRole.Tutor,
                    Level = EducationLevel.DaiHoc, // Mặc định cấp Đại học
                    IsApproved = true, // Tự động phê duyệt vì được tạo bởi School
                    RegisteredDate = DateTime.Now,
                    Bio = bio ?? "",
                    Subjects = subjects ?? "",
                    Education = education ?? "",
                    ExperienceYears = experienceYears,
                    Certificates = certificates ?? ""
                };

                if (_userService.Register(mentor))
                {
                    TempData["Success"] = $"Tạo tài khoản Mentor thành công! Tên đăng nhập: {username}";
                    return RedirectToAction("ManageMentors");
                }
                else
                {
                    TempData["Error"] = "Không thể tạo tài khoản Mentor. Vui lòng thử lại!";
                    return RedirectToAction("CreateMentor");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateMentor POST: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo tài khoản Mentor: {ex.Message}";
                return RedirectToAction("CreateMentor");
            }
        }

        // ============================================================
        // END MENTOR MANAGEMENT ACTIONS
        // ============================================================
    }
}
