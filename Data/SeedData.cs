using System;
using System.Linq;
using Webgiasu.Models;

namespace Webgiasu.Data
{
    public static class SeedData
    {
        public static void EnsureSeedData(AppDbContext db)
        {
            // Không seed nếu đã có dữ liệu
            if (db.Users.Any()) return;

            var now = DateTime.Now;

            // ========== USERS ==========
            var student = new User
            {
                Username = "student1",
                Password = "password",
                FullName = "Nguyễn Văn A",
                Email = "student1@example.com",
                PhoneNumber = "0911000001",
                Role = UserRole.Student,
                IsApproved = true,
                RegisteredDate = now.AddDays(-10)
            };

            var tutor1 = new User
            {
                Username = "tutor1",
                Password = "password",
                FullName = "Trần Thị B",
                Email = "tutor1@example.com",
                PhoneNumber = "0911000002",
                Role = UserRole.Tutor,
                IsApproved = true,
                IsPremium = true,
                Subjects = "Toán, Lý",
                RegisteredDate = now.AddDays(-20)
            };

            var tutor2 = new User
            {
                Username = "tutor2",
                Password = "password",
                FullName = "Lê Văn C",
                Email = "tutor2@example.com",
                PhoneNumber = "0911000003",
                Role = UserRole.Tutor,
                IsApproved = true,
                RegisteredDate = now.AddDays(-15)
            };

            var school = new User
            {
                Username = "school1",
                Password = "password",
                FullName = "Trường THPT ABC",
                Email = "school@example.com",
                PhoneNumber = "0911999999",
                Role = UserRole.School,
                IsApproved = true,
                RegisteredDate = now.AddYears(-1)
            };

            var admin = new User
            {
                Username = "admin",
                Password = "admin",
                FullName = "Administrator",
                Email = "admin@example.com",
                PhoneNumber = "0900000000",
                Role = UserRole.Admin,
                IsApproved = true,
                RegisteredDate = now.AddYears(-2)
            };

            db.Users.AddRange(student, tutor1, tutor2, school, admin);
            db.SaveChanges();

            // ========== FRIENDSHIPS & MESSAGES ==========
            var f1 = new Friendship
            {
                RequesterId = student.Id,
                AddresseeId = tutor1.Id,
                Status = FriendshipStatus.Accepted,
                RequestedDate = now.AddDays(-5),
                RespondedDate = now.AddDays(-4)
            };
            db.Friendships.Add(f1);

            db.Messages.AddRange(
                new Message
                {
                    SenderId = tutor1.Id,
                    ReceiverId = student.Id,
                    Content = "Chào bạn, mình có thể nhận bài này nhé.",
                    SentDate = now.AddDays(-3),
                    IsRead = true,
                    Type = 0
                },
                new Message
                {
                    SenderId = student.Id,
                    ReceiverId = tutor1.Id,
                    Content = "Cảm ơn, mình đã gửi yêu cầu.",
                    SentDate = now.AddDays(-2),
                    IsRead = true,
                    Type = 0
                }
            );
            db.SaveChanges();

            // ========== PROBLEMS ==========
            var p1 = new Problem
            {
                StudentId = student.Id,
                Title = "Bài toán Toán cơ bản - Tính tích",
                Description = "Giải phương trình và nêu lời giải chi tiết.",
                Type = ProblemType.Toan,
                Difficulty = DifficultyLevel.THPT,
                ImageUrl = "/images/default.jpg",
                CreatedDate = now.AddDays(-7),
                Deadline = now.AddDays(3),
                Status = ProblemStatus.Solved,
                AssignedTutorId = tutor1.Id,
                Price = 50000
            };

            var p2 = new Problem
            {
                StudentId = student.Id,
                Title = "Bài vật lý: chuyển động",
                Description = "Tính vận tốc trung bình.",
                Type = ProblemType.Ly,
                Difficulty = DifficultyLevel.THCS,
                ImageUrl = "/images/default.jpg",
                CreatedDate = now.AddDays(-2),
                Deadline = now.AddDays(5),
                Status = ProblemStatus.WaitingForTutor,
                Price = 30000
            };

            db.Problems.AddRange(p1, p2);
            db.SaveChanges();

            // ========== SOLUTION + PAYMENT + RATING ==========
            var sol1 = new Solution
            {
                ProblemId = p1.Id,
                TutorId = tutor1.Id,
                Content = "Lời giải chi tiết ...",
                FileUrl = "/files/solution1.pdf",
                SubmittedDate = now.AddDays(-6),
                Rating = 5,
                Feedback = "Rất rõ ràng"
            };
            db.Solutions.Add(sol1);

            var pay1 = new Payment
            {
                StudentId = student.Id,
                ProblemId = p1.Id,
                Amount = p1.Price,
                Status = PaymentStatus.Completed,
                CreatedDate = now.AddDays(-6),
                CompletedDate = now.AddDays(-5),
                TransactionId = $"TXN{now.Ticks}"
            };
            db.Payments.Add(pay1);

            var rating = new Rating
            {
                ProblemId = p1.Id,
                StudentId = student.Id,
                TutorId = tutor1.Id,
                Stars = 5,
                Comment = "Lời giải rất tốt, dễ hiểu",
                CreatedAt = now.AddDays(-4)
            };
            db.Ratings.Add(rating);
            db.SaveChanges();

            // ========== PROBLEM GROUP (NHÓM) ==========
            var groupProblem = new Problem
            {
                StudentId = student.Id,
                Title = "Nhóm: Bài toán chia tiền thuê tutor",
                Description = "Bài toán nhóm để share chi phí",
                Type = ProblemType.Khac,
                Difficulty = DifficultyLevel.THPT,
                ImageUrl = "/images/default.jpg",
                CreatedDate = now,
                Deadline = now.AddDays(7),
                Status = ProblemStatus.WaitingForTutor,
                Price = 150000
            };
            db.Problems.Add(groupProblem);
            db.SaveChanges();

            var group = new ProblemGroup
            {
                ProblemId = groupProblem.Id,
                CreatedByUserId = student.Id,
                GroupName = "Nhóm Học Toán",
                TotalPrice = 150000m,
                PricePerMember = 150000m, // sẽ update sau
                Status = ProblemGroupStatus.Open,
                CreatedAt = now
            };
            db.ProblemGroups.Add(group);
            db.SaveChanges();

            // add members
            var memberOwner = new ProblemGroupMember
            {
                GroupId = group.Id,
                UserId = student.Id,
                Role = GroupMemberRole.Owner,
                PaymentStatus = GroupPaymentStatus.Unpaid,
                JoinedAt = now
            };
            var memberTutor2 = new ProblemGroupMember
            {
                GroupId = group.Id,
                UserId = tutor2.Id,
                Role = GroupMemberRole.Member,
                PaymentStatus = GroupPaymentStatus.Unpaid,
                JoinedAt = now
            };
            db.ProblemGroupMembers.AddRange(memberOwner, memberTutor2);
            db.SaveChanges();

            // update price per member
            group.PricePerMember = Math.Round(group.TotalPrice / db.ProblemGroupMembers.Count(m => m.GroupId == group.Id), 0);
            db.SaveChanges();

            // create a group payment for owner (pending)
            var gp = new GroupPayment
            {
                GroupId = group.Id,
                MemberId = memberOwner.Id,
                UserId = student.Id,
                Amount = group.PricePerMember,
                Status = PaymentStatus.Pending,
                CreatedDate = now
            };
            db.GroupPayments.Add(gp);
            db.SaveChanges();

            // ========== COMMUNITY ==========
            var post = new CommunityPost
            {
                UserId = student.Id,
                Title = "Mẹo giải bài Toán nhanh",
                Content = "Một số mẹo và lưu ý khi giải bài...",
                CreatedAt = now,
                IsDeleted = false,
                IsLocked = false,
                Category = "Toán"
            };
            db.CommunityPosts.Add(post);
            db.SaveChanges();

            var comment = new CommunityComment
            {
                PostId = post.Id,
                UserId = tutor1.Id,
                Content = "Gợi ý rất hay, cảm ơn bạn!",
                IsAnonymous = false,
                CreatedAt = now
            };
            db.CommunityComments.Add(comment);
            db.SaveChanges();

            // ========== SCHOOL CLASS ==========
            var schoolClass = new SchoolClass
            {
                SchoolId = school.Id,
                ClassName = "Lớp 12A1 - Toán",
                Subject = "Toán",
                Description = "Lớp học ôn thi",
                TutorId = tutor1.Id,
                StartDate = now.AddDays(-30),
                EndDate = now.AddMonths(3),
                CreatedDate = now.AddDays(-30),
                Status = ClassStatus.Active,
                MeetingType = "Online"
            };
            db.SchoolClasses.Add(schoolClass);
            db.SaveChanges();

            db.ClassStudents.Add(new ClassStudent
            {
                ClassId = schoolClass.Id,
                StudentId = student.Id,
                JoinedDate = now.AddDays(-29)
            });
            db.SaveChanges();

            db.ClassSchedules.Add(new ClassSchedule
            {
                ClassId = schoolClass.Id,
                Title = "Buổi ôn tập: Hàm số",
                ScheduleDate = now.AddDays(2),
                StartTime = new TimeSpan(19, 0, 0),
                EndTime = new TimeSpan(21, 0, 0),
                MeetingType = "Online",
                Location = "Zoom",
                Content = "Ôn tập chuyên đề hàm số",
                Notes = "Chuẩn bị đề mẫu",
                Status = ScheduleStatus.Upcoming,
                CreatedDate = now,
                CreatedBy = school.Id
            });
            db.SaveChanges();

            // ========== TUTOR APPLICATION ==========
            var app1 = new TutorApplication
            {
                ProblemId = p2.Id,
                TutorId = tutor2.Id,
                Proposal = "Mình nộp đơn, sẽ hoàn thành trong 2 ngày",
                ProposedPrice = 28000,
                EstimatedDays = 2,
                Status = ApplicationStatus.Pending,
                AppliedDate = now
            };
            db.TutorApplications.Add(app1);
            db.SaveChanges();

            // ========== NOTIFICATION SAMPLE ==========
            db.Notifications.Add(new Notification
            {
                UserId = student.Id,
                Type = NotificationType.ProblemCreated,
                Title = "Đăng bài thành công",
                Message = $"Bài '{p2.Title}' đã được đăng",
                Link = $"/Student/ProblemDetails?id={p2.Id}",
                ProblemId = p2.Id,
                CreatedDate = now
            });
            db.SaveChanges();
        }
    }
}