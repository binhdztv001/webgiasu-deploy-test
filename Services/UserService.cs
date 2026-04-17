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

        private static bool IsBcryptHash(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$");
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(User user, string password)
        {
            if (user == null || string.IsNullOrWhiteSpace(password)) return false;

            if (IsBcryptHash(user.Password))
            {
                return BCrypt.Net.BCrypt.Verify(password, user.Password);
            }

            return user.Password == password;
        }

        public User? Login(string username, string password, UserRole role)
        {
            try
            {
                var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Role == role);
                if (user == null)
                {
                    return null;
                }

                if (!VerifyPassword(user, password))
                {
                    return null;
                }

                // Tự động nâng cấp mật khẩu cũ (plain text) sang BCrypt sau lần đăng nhập thành công.
                if (!IsBcryptHash(user.Password))
                {
                    user.Password = HashPassword(password);
                    _db.SaveChanges();
                }

                return user;
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

                if (!string.IsNullOrWhiteSpace(user.Password) && !IsBcryptHash(user.Password))
                {
                    user.Password = HashPassword(user.Password);
                }

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
                    .Where(u => u.Role == UserRole.Tutor && !u.IsApproved)  
                    .OrderBy(u => u.RegisteredDate)  
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetPendingTutors: {ex.Message}");
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

                if (!string.IsNullOrWhiteSpace(user.Password))
                {
                    if (IsBcryptHash(user.Password))
                    {
                        existing.Password = user.Password;
                    }
                    else
                    {
                        existing.Password = HashPassword(user.Password);
                    }
                }

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

        public bool DeleteUser(int userId)
        {
            try
            {
                var user = _db.Users.Find(userId);
                if (user == null) return false;

                _db.Users.Remove(user);
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deleting user: {ex.Message}");
                return false;
            }
        }
    }
}
