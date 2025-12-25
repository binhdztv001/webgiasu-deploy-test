using System.Collections.Generic;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface IUserService
    {
        User? Login(string username, string password, UserRole role);
        bool Register(User user);
        User? GetUserById(int id);
        List<User> GetAllUsers();
        List<User> GetPendingTutors();
        bool ApproveTutor(int tutorId);
        List<User> GetUsersByRole(UserRole role);
        bool UpdateUser(User user);
    }
}
