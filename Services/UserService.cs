using System;
using System.Collections.Generic;
using System.Linq;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;
        public UserService(AppDbContext db) => _db = db;

        public User? Login(string username, string password, UserRole role)
        {
            try
            {
                return _db.Users.FirstOrDefault(u => u.Username == username && u.Password == password && u.Role == role);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public bool Register(User user)
        {
            try
            {
                if (_db.Users.Any(u => u.Username == user.Username))
                    return false;

                user.RegisteredDate = DateTime.Now;
                // Chỉ tự động phê duyệt nếu IsApproved chưa được set (mặc định cho Student)
                // Nếu đã được set từ bên ngoài (ví dụ School tạo Mentor), giữ nguyên giá trị đó
                if (!user.IsApproved && user.Role == UserRole.Student)
                {
                    user.IsApproved = true;
                }
                
                _db.Users.Add(user);
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //maaux
        public User? GetUserById(int id)
        {
            try
            {
                return _db.Users.Find(id);
            }

            catch (Exception ex)
            {
                return new User();
            }
        }

        public List<User> GetAllUsers()
        {
            try
            {
                return _db.Users.ToList();
            }
            catch (Exception ex)
            {
                return new List<User>();
            }
        }

        public List<User> GetPendingTutors()
        {
            try
            {
                return _db.Users
                    .Where(u => u.Role == UserRole.Tutor && u.IsApproved)
                    .OrderByDescending(u => u.IsPremium)
                    .ThenByDescending(u => u.ExperienceYears)
                    .ThenBy(u => u.FullName)
                    .ToList();
            }
            catch (Exception ex)
            {
                return new List<User>();
            }
        }

        public bool ApproveTutor(int tutorId)
        {
            try
            {
                var tutor = _db.Users.FirstOrDefault(u => u.Id == tutorId && u.Role == UserRole.Tutor);
                if (tutor == null) return false;
                tutor.IsApproved = true;
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<User> GetUsersByRole(UserRole role)
        {
            try
            {
                return _db.Users.Where(u => u.Role == role).ToList();
            }
            catch (Exception ex)
            {
                return new List<User>();
            }
        }

        public bool UpdateUser(User user)
        {
            try
            {
                var existing = _db.Users.Find(user.Id);
                if (existing == null) return false;

                existing.FullName = user.FullName;
                existing.Email = user.Email;
                existing.PhoneNumber = user.PhoneNumber;
                existing.Password = user.Password;
                existing.IsApproved = user.IsApproved;

                // ✅ UPDATE LEVEL
                existing.Level = user.Level;

                // Tutor profile fields
                existing.Bio = user.Bio;
                existing.Subjects = user.Subjects;
                existing.Education = user.Education;
                existing.ExperienceYears = user.ExperienceYears;
                existing.Certificates = user.Certificates;

                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating user: {ex.Message}");
                return false;
            }
        }
    }
}
