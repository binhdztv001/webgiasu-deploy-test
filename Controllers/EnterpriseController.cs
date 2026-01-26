using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models;
using Webgiasu.Services;
using Microsoft.EntityFrameworkCore;
using Webgiasu.Models.ViewModels;

namespace Webgiasu.Controllers
{
    public class EnterpriseController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IUserService _userService;
        private readonly ISchoolClassService _classService;
        private readonly IStatisticsExportService _statisticsExportService;

        public EnterpriseController(AppDbContext db, IUserService userService, ISchoolClassService classService, IStatisticsExportService statisticsExportService)
        {
            _db = db;
            _userService = userService;
            _classService = classService;
            _statisticsExportService = statisticsExportService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user == null || user.Role != UserRole.Enterprise)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Thống kê tổng quan
                // Đếm số mentor đã tạo
                var totalMentors = _userService.GetUsersByRole(UserRole.Tutor)
                    .Count(m => m.Level.HasValue && m.Level.Value == EducationLevel.DaiHoc && m.IsApproved);
                ViewBag.TotalMentors = totalMentors;

                // Đếm số lớp học của các trường (tất cả trường)
                var allSchools = _userService.GetUsersByRole(UserRole.School);
                var totalClasses = 0;
                var activeClasses = 0;
                foreach (var school in allSchools)
                {
                    var classes = _classService.GetClassesBySchoolId(school.Id);
                    totalClasses += classes.Count;
                    activeClasses += classes.Count(c => c.Status == ClassStatus.Active);
                }
                ViewBag.TotalClasses = totalClasses;
                ViewBag.ActiveClasses = activeClasses;

                // Đếm số lịch trao đổi (của tất cả trường)
                var totalSchedules = _db.ClassSchedules.Count();
                ViewBag.TotalSchedules = totalSchedules;

                // Đếm số trường
                ViewBag.TotalSchools = allSchools.Count;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Enterprise Dashboard: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang chủ!";
                return RedirectToAction("Index", "Home");
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
                    IsApproved = true, // Tự động phê duyệt vì được tạo bởi Enterprise
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

        // Xem thông tin lớp học của tất cả trường
        public IActionResult SchoolClasses()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Lấy tất cả trường
                var allSchools = _userService.GetUsersByRole(UserRole.School);
                
                var allClasses = new List<dynamic>();
                foreach (var school in allSchools)
                {
                    var classes = _classService.GetClassesBySchoolId(school.Id);
                    foreach (var c in classes)
                    {
                        allClasses.Add(new
                        {
                            ClassId = c.Id,
                            Id = c.Id,
                            ClassName = c.ClassName,
                            Subject = c.Subject,
                            SchoolName = school.FullName,
                            TutorName = c.TutorId.HasValue ? _userService.GetUserById(c.TutorId.Value)?.FullName ?? "Chưa có" : "Chưa có",
                            StudentCount = _classService.GetClassStudentIds(c.Id).Count,
                            StartDate = c.StartDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                            Status = c.Status,
                            StatusText = c.Status == ClassStatus.Active ? "Đang hoạt động" : 
                                       c.Status == ClassStatus.Completed ? "Đã hoàn thành" : "Đã hủy"
                        });
                    }
                }

                ViewBag.AllClasses = allClasses.OrderByDescending(c => c.StartDate).ToList();

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SchoolClasses: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách lớp học!";
                return RedirectToAction("Dashboard");
            }
        }

        // Xem lịch trao đổi của tất cả trường
        public IActionResult SchoolSchedules()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Lấy tất cả lịch trao đổi của tất cả trường
                var allSchedules = _db.ClassSchedules
                    .Include(s => s.Class)
                    .ThenInclude(c => c.School)
                    .OrderByDescending(s => s.ScheduleDate)
                    .Select(s => new
                    {
                        ScheduleId = s.Id,
                        Id = s.Id,
                        Title = s.Title,
                        ScheduleDate = s.ScheduleDate,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        MeetingType = s.MeetingType,
                        Location = s.Location ?? "Chưa xác định",
                        Status = s.Status,
                        StatusText = s.Status == ScheduleStatus.Upcoming ? "Sắp diễn ra" :
                                   s.Status == ScheduleStatus.InProgress ? "Đang diễn ra" :
                                   s.Status == ScheduleStatus.Completed ? "Đã hoàn thành" : "Đã hủy",
                        ClassName = s.Class!.ClassName,
                        SchoolName = s.Class.School!.FullName,
                        Subject = s.Class.Subject
                    })
                    .ToList();

                ViewBag.AllSchedules = allSchedules;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SchoolSchedules: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách lịch trao đổi!";
                return RedirectToAction("Dashboard");
            }
        }

        // Chi tiết lớp học
        public IActionResult ClassDetail(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Tìm lớp học
                var schoolClass = _db.SchoolClasses
                    .Include(c => c.School)
                    .Include(c => c.Tutor)
                    .FirstOrDefault(c => c.Id == id);

                if (schoolClass == null)
                {
                    TempData["Error"] = "Không tìm thấy lớp học!";
                    return RedirectToAction("SchoolClasses");
                }

                // Lấy danh sách học sinh
                var studentIds = _classService.GetClassStudentIds(id);
                var students = studentIds.Select(sid => _userService.GetUserById(sid)).Where(s => s != null).ToList();

                ViewBag.Class = schoolClass;
                ViewBag.Students = students;
                ViewBag.School = schoolClass.School;
                ViewBag.Tutor = schoolClass.Tutor;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ClassDetail: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("SchoolClasses");
            }
        }

        // Chi tiết lịch trao đổi
        public IActionResult ScheduleDetail(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Tìm lịch trao đổi
                var schedule = _db.ClassSchedules
                    .Include(s => s.Class)
                    .ThenInclude(c => c.School)
                    .Include(s => s.Class)
                    .ThenInclude(c => c.Tutor)
                    .FirstOrDefault(s => s.Id == id);

                if (schedule == null)
                {
                    TempData["Error"] = "Không tìm thấy lịch trao đổi!";
                    return RedirectToAction("SchoolSchedules");
                }

                ViewBag.Schedule = schedule;
                ViewBag.Class = schedule.Class;
                ViewBag.School = schedule.Class?.School;
                ViewBag.Tutor = schedule.Class?.Tutor;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ScheduleDetail: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("SchoolSchedules");
            }
        }

        // Messages
        public IActionResult Messages()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // TODO: Implement messaging feature
                TempData["Info"] = "Tính năng tin nhắn đang được phát triển!";
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Messages: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        // Statistics
        public IActionResult Statistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                // Thống kê chi tiết
                // Số mentor đã tạo
                var allMentors = _userService.GetUsersByRole(UserRole.Tutor)
                    .Where(m => m.Level.HasValue && m.Level.Value == EducationLevel.DaiHoc && m.IsApproved)
                    .ToList();
                ViewBag.TotalMentors = allMentors.Count;

                // Thống kê theo trường
                var allSchools = _userService.GetUsersByRole(UserRole.School);
                ViewBag.TotalSchools = allSchools.Count;

                var schoolStats = new List<dynamic>();
                var totalClasses = 0;
                var activeClasses = 0;
                var totalStudents = 0;
                var totalSchedules = 0;

                foreach (var school in allSchools)
                {
                    var classes = _classService.GetClassesBySchoolId(school.Id);
                    var schoolClasses = classes.Count;
                    var schoolActiveClasses = classes.Count(c => c.Status == ClassStatus.Active);
                    
                    var schoolStudents = 0;
                    foreach (var c in classes)
                    {
                        schoolStudents += _classService.GetClassStudentIds(c.Id).Count;
                    }

                    var schoolSchedules = _db.ClassSchedules
                        .Count(s => s.Class!.SchoolId == school.Id);

                    totalClasses += schoolClasses;
                    activeClasses += schoolActiveClasses;
                    totalStudents += schoolStudents;
                    totalSchedules += schoolSchedules;

                    schoolStats.Add(new
                    {
                        SchoolName = school.FullName,
                        TotalClasses = schoolClasses,
                        ActiveClasses = schoolActiveClasses,
                        TotalStudents = schoolStudents,
                        TotalSchedules = schoolSchedules
                    });
                }

                ViewBag.TotalClasses = totalClasses;
                ViewBag.ActiveClasses = activeClasses;
                ViewBag.TotalStudents = totalStudents;
                ViewBag.TotalSchedules = totalSchedules;
                ViewBag.SchoolStats = schoolStats;

                // Thống kê mentor theo môn học
                var mentorsBySubject = allMentors
                    .Where(m => !string.IsNullOrEmpty(m.Subjects))
                    .GroupBy(m => m.Subjects)
                    .Select(g => new { Subject = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList();
                ViewBag.MentorsBySubject = (IEnumerable<dynamic>)mentorsBySubject;

                // Thống kê lớp học theo tháng (6 tháng gần nhất)
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var allClassesFromAllSchools = new List<SchoolClass>();
                foreach (var school in allSchools)
                {
                    allClassesFromAllSchools.AddRange(_classService.GetClassesBySchoolId(school.Id));
                }

                var classesByMonth = allClassesFromAllSchools
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

                // Thống kê lịch trao đổi theo trạng thái
                var schedulesByStatus = new List<dynamic>
                {
                    new { Status = "Sắp diễn ra", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.Upcoming) },
                    new { Status = "Đang diễn ra", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.InProgress) },
                    new { Status = "Đã hoàn thành", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.Completed) },
                    new { Status = "Đã hủy", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.Cancelled) }
                };
                ViewBag.SchedulesByStatus = (IEnumerable<dynamic>)schedulesByStatus;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Statistics: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        // Profile
        public IActionResult Profile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);

                // Thống kê cho Profile
                var totalMentors = _userService.GetUsersByRole(UserRole.Tutor)
                    .Count(m => m.Level.HasValue && m.Level.Value == EducationLevel.DaiHoc && m.IsApproved);
                ViewBag.TotalMentors = totalMentors;

                var allSchools = _userService.GetUsersByRole(UserRole.School);
                var totalClasses = 0;
                foreach (var school in allSchools)
                {
                    var classes = _classService.GetClassesBySchoolId(school.Id);
                    totalClasses += classes.Count;
                }
                ViewBag.TotalClasses = totalClasses;

                var totalSchedules = _db.ClassSchedules.Count();
                ViewBag.TotalSchedules = totalSchedules;

                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Profile: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi!";
                return RedirectToAction("Dashboard");
            }
        }

        // Settings
        public IActionResult Settings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                return View(user);
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

                var user = _userService.GetUserById(userId);
                if (user != null)
                {
                    user.FullName = fullName;
                    user.Email = email;
                    user.PhoneNumber = phoneNumber;

                    _userService.UpdateUser(user);
                    HttpContext.Session.SetString("UserName", user.FullName);

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

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                if (user != null)
                {
                    if (user.Password == currentPassword)
                    {
                        user.Password = newPassword;
                        _userService.UpdateUser(user);
                        TempData["Success"] = "Đổi mật khẩu thành công!";
                    }
                    else
                    {
                        TempData["Error"] = "Mật khẩu hiện tại không đúng!";
                    }
                }
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ChangePassword: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi đổi mật khẩu!";
                return RedirectToAction("Settings");
            }
        }

        [HttpGet]
        public IActionResult ExportStatisticsToPdf()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                
                // Lấy dữ liệu từ Statistics action
                var allMentors = _userService.GetUsersByRole(UserRole.Tutor)
                    .Where(m => m.Level.HasValue && m.Level.Value == EducationLevel.DaiHoc && m.IsApproved)
                    .ToList();
                var allSchools = _userService.GetUsersByRole(UserRole.School);

                var totalClasses = 0;
                var activeClasses = 0;
                var totalStudents = 0;
                var totalSchedules = 0;

                var schoolStats = new List<SchoolStatisticDetail>();
                foreach (var school in allSchools)
                {
                    var classes = _classService.GetClassesBySchoolId(school.Id);
                    var schoolClasses = classes.Count;
                    var schoolActiveClasses = classes.Count(c => c.Status == ClassStatus.Active);
                    
                    var schoolStudents = 0;
                    foreach (var c in classes)
                    {
                        schoolStudents += _classService.GetClassStudentIds(c.Id).Count;
                    }

                    var schoolSchedules = _db.ClassSchedules.Count(s => s.Class!.SchoolId == school.Id);

                    totalClasses += schoolClasses;
                    activeClasses += schoolActiveClasses;
                    totalStudents += schoolStudents;
                    totalSchedules += schoolSchedules;

                    schoolStats.Add(new SchoolStatisticDetail
                    {
                        SchoolName = school.FullName,
                        TotalClasses = schoolClasses,
                        ActiveClasses = schoolActiveClasses,
                        TotalStudents = schoolStudents,
                        TotalSchedules = schoolSchedules
                    });
                }

                var data = new EnterpriseStatisticsExportModel
                {
                    TotalSchools = allSchools.Count,
                    TotalMentors = allMentors.Count,
                    TotalClasses = totalClasses,
                    ActiveClasses = activeClasses,
                    TotalStudents = totalStudents,
                    TotalSchedules = totalSchedules,
                    SchoolStats = schoolStats,

                    MentorsBySubject = allMentors
                        .Where(m => !string.IsNullOrEmpty(m.Subjects))
                        .GroupBy(m => m.Subjects)
                        .Select(g => new SubjectStatistic { Subject = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToList(),

                    SchedulesByStatus = new List<StatusStatistic>
                    {
                        new StatusStatistic { Status = "Sắp diễn ra", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.Upcoming) },
                        new StatusStatistic { Status = "Đang diễn ra", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.InProgress) },
                        new StatusStatistic { Status = "Đã hoàn thành", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.Completed) },
                        new StatusStatistic { Status = "Đã hủy", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.Cancelled) }
                    },

                    ClassesByMonth = new List<MonthStatistic>() // Có thể bỏ trống hoặc tính toán nếu cần
                };

                var pdfBytes = _statisticsExportService.ExportEnterpriseStatisticsToPdf(data, user?.FullName ?? "Enterprise");
                
                return File(pdfBytes, "application/pdf", $"ThongKeHeThong_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ExportStatisticsToPdf: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Đã xảy ra lỗi khi xuất PDF!";
                return RedirectToAction("Statistics");
            }
        }

        [HttpGet]
        public IActionResult ExportStatisticsToExcel()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var user = _userService.GetUserById(userId);
                
                var allMentors = _userService.GetUsersByRole(UserRole.Tutor)
                    .Where(m => m.Level.HasValue && m.Level.Value == EducationLevel.DaiHoc && m.IsApproved)
                    .ToList();
                var allSchools = _userService.GetUsersByRole(UserRole.School);

                var totalClasses = 0;
                var activeClasses = 0;
                var totalStudents = 0;
                var totalSchedules = 0;

                var schoolStats = new List<SchoolStatisticDetail>();
                foreach (var school in allSchools)
                {
                    var classes = _classService.GetClassesBySchoolId(school.Id);
                    var schoolClasses = classes.Count;
                    var schoolActiveClasses = classes.Count(c => c.Status == ClassStatus.Active);
                    
                    var schoolStudents = 0;
                    foreach (var c in classes)
                    {
                        schoolStudents += _classService.GetClassStudentIds(c.Id).Count;
                    }

                    var schoolSchedules = _db.ClassSchedules.Count(s => s.Class!.SchoolId == school.Id);

                    totalClasses += schoolClasses;
                    activeClasses += schoolActiveClasses;
                    totalStudents += schoolStudents;
                    totalSchedules += schoolSchedules;

                    schoolStats.Add(new SchoolStatisticDetail
                    {
                        SchoolName = school.FullName,
                        TotalClasses = schoolClasses,
                        ActiveClasses = schoolActiveClasses,
                        TotalStudents = schoolStudents,
                        TotalSchedules = schoolSchedules
                    });
                }

                var data = new EnterpriseStatisticsExportModel
                {
                    TotalSchools = allSchools.Count,
                    TotalMentors = allMentors.Count,
                    TotalClasses = totalClasses,
                    ActiveClasses = activeClasses,
                    TotalStudents = totalStudents,
                    TotalSchedules = totalSchedules,
                    SchoolStats = schoolStats,

                    MentorsBySubject = allMentors
                        .Where(m => !string.IsNullOrEmpty(m.Subjects))
                        .GroupBy(m => m.Subjects)
                        .Select(g => new SubjectStatistic { Subject = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToList(),

                    SchedulesByStatus = new List<StatusStatistic>
                    {
                        new StatusStatistic { Status = "Sắp diễn ra", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.Upcoming) },
                        new StatusStatistic { Status = "Đang diễn ra", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.InProgress) },
                        new StatusStatistic { Status = "Đã hoàn thành", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.Completed) },
                        new StatusStatistic { Status = "Đã hủy", Count = _db.ClassSchedules.Count(s => s.Status == ScheduleStatus.Cancelled) }
                    },

                    ClassesByMonth = new List<MonthStatistic>()
                };

                var excelBytes = _statisticsExportService.ExportEnterpriseStatisticsToExcel(data, user?.FullName ?? "Enterprise");
                
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    $"ThongKeHeThong_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ExportStatisticsToExcel: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Đã xảy ra lỗi khi xuất Excel!";
                return RedirectToAction("Statistics");
            }
        }
    }
}
