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
            => _db.Users.FirstOrDefault(u => u.Username == username && u.Password == password && u.Role == role);

        public bool Register(User user)
        {
            if (_db.Users.Any(u => u.Username == user.Username))
                return false;

            user.RegisteredDate = DateTime.Now;
            user.IsApproved = user.Role == UserRole.Student;
            _db.Users.Add(user);
            _db.SaveChanges();
            return true;
        }

        //maaux
        public User? GetUserById(int id)
        {
            try
            {
                return _db.Users.Find(id);
            }

            catch
            {
                return new User();
            }
        }

        public List<User> GetAllUsers() => _db.Users.ToList();

        public List<User> GetPendingTutors() => _db.Users.Where(u => u.Role == UserRole.Tutor && !u.IsApproved).ToList();

        public bool ApproveTutor(int tutorId)
        {
            var tutor = _db.Users.FirstOrDefault(u => u.Id == tutorId && u.Role == UserRole.Tutor);
            if (tutor == null) return false;
            tutor.IsApproved = true;
            _db.SaveChanges();
            return true;
        }

        public List<User> GetUsersByRole(UserRole role) => _db.Users.Where(u => u.Role == role).ToList();

        public bool UpdateUser(User user)
        {
            var existing = _db.Users.Find(user.Id);
            if (existing == null) return false;

            existing.FullName = user.FullName;
            existing.Email = user.Email;
            existing.PhoneNumber = user.PhoneNumber;
            existing.Password = user.Password;
            existing.IsApproved = user.IsApproved;

            existing.Bio = user.Bio;
            existing.Subjects = user.Subjects;
            existing.Education = user.Education;
            existing.ExperienceYears = user.ExperienceYears;
            existing.Certificates = user.Certificates;

            _db.SaveChanges();
            return true;
        }
    }
}
