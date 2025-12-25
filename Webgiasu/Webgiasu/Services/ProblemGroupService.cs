
using Webgiasu.Models;
using Microsoft.EntityFrameworkCore;

namespace Webgiasu.Services
{
    public class ProblemGroupService : IProblemGroupService
    {
        private readonly AppDbContext _context;

        public ProblemGroupService(AppDbContext context)
        {
            _context = context;
        }

        // ==================== GROUP MANAGEMENT ====================

        public ProblemGroup? CreateProblemGroup(int problemId, int createdByUserId, string? groupName, decimal totalPrice)
        {
            try
            {
                var group = new ProblemGroup
                {
                    ProblemId = problemId,
                    CreatedByUserId = createdByUserId,
                    GroupName = groupName ?? "Nhóm học tập",
                    TotalPrice = totalPrice,
                    PricePerMember = totalPrice, // Initially 1 member (creator)
                    Status = ProblemGroupStatus.Open,
                    CreatedAt = DateTime.Now
                };

                _context.ProblemGroups.Add(group);
                _context.SaveChanges();

                // Add creator as first member
                AddMember(group.Id, createdByUserId, GroupMemberRole.Owner);

                return group;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating group: {ex.Message}");
                return null;
            }
        }

        public ProblemGroup? GetGroupById(int groupId)
        {
            try
            {
                var group = _context.ProblemGroups
                    .Include(g => g.Problem)
                    .Include(g => g.CreatedByUser)
                    .Include(g => g.Members)
                        .ThenInclude(m => m.User)
                    .Include(g => g.Invites)
                        .ThenInclude(i => i.InvitedUser)
                    .FirstOrDefault(g => g.Id == groupId);

                if (group == null)
                {
                    Console.WriteLine($"❌ GetGroupById: Group {groupId} not found in database");
                }
                else
                {
                    Console.WriteLine($"✅ GetGroupById: Found group {group.Id} - {group.GroupName}");
                }

                return group;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetGroupById: {ex.Message}");
                return null;
            }
        }

        public ProblemGroup? GetGroupByProblemId(int problemId)
        {
            return _context.ProblemGroups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Include(g => g.Invites)
                .FirstOrDefault(g => g.ProblemId == problemId);
        }

        public List<ProblemGroup> GetGroupsByUserId(int userId)
        {
            try
            {
                Console.WriteLine($"📥 GetGroupsByUserId called for userId={userId}");

                // ✅ Chỉ lấy nhóm mà user là thành viên (có trong bảng Members)
                var groups = _context.ProblemGroups
                    .Include(g => g.Problem)                    // Load Problem info
                    .Include(g => g.CreatedByUser)              // Load creator info
                    .Include(g => g.Members)                    // Load all members
                        .ThenInclude(m => m.User)               // Load User for each member
                    .Include(g => g.Invites)                    // Load invites
                        .ThenInclude(i => i.InvitedUser)        // Load invited user info
                    .Where(g => g.Members.Any(m => m.UserId == userId))  // ✅ CHỈ lấy nhóm có user trong Members
                    .OrderByDescending(g => g.CreatedAt)
                    .ToList();

                Console.WriteLine($"✅ Found {groups.Count} groups where user {userId} is a member");

                // Debug log
                foreach (var group in groups)
                {
                    Console.WriteLine($"  Group {group.Id}: {group.GroupName}, Members: {group.Members.Count}");
                    var userMember = group.Members.FirstOrDefault(m => m.UserId == userId);
                    if (userMember != null)
                    {
                        Console.WriteLine($"    User role: {userMember.Role}, Joined: {userMember.JoinedAt}");
                    }
                }

                return groups;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetGroupsByUserId: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<ProblemGroup>();
            }
        }

        public bool UpdateGroupStatus(int groupId, ProblemGroupStatus status)
        {
            try
            {
                var group = _context.ProblemGroups.Find(groupId);
                if (group == null) return false;

                group.Status = status;
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteGroup(int groupId)
        {
            try
            {
                var group = _context.ProblemGroups.Find(groupId);
                if (group == null) return false;

                _context.ProblemGroups.Remove(group);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ==================== MEMBER MANAGEMENT ====================

        public bool AddMember(int groupId, int userId, GroupMemberRole role)
        {
            try
            {
                // Check if already member
                var existing = _context.ProblemGroupMembers
                    .FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId);

                if (existing != null) return false;

                var member = new ProblemGroupMember
                {
                    GroupId = groupId,
                    UserId = userId,
                    Role = role,
                    PaymentStatus = GroupPaymentStatus.Unpaid,
                    JoinedAt = DateTime.Now
                };

                _context.ProblemGroupMembers.Add(member);
                _context.SaveChanges();

                // Update prices
                UpdateGroupPrices(groupId);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveMember(int groupId, int userId)
        {
            try
            {
                var member = _context.ProblemGroupMembers
                    .FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId);

                if (member == null) return false;

                // Cannot remove owner
                if (member.Role == GroupMemberRole.Owner) return false;

                _context.ProblemGroupMembers.Remove(member);
                _context.SaveChanges();

                // Update prices
                UpdateGroupPrices(groupId);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<ProblemGroupMember> GetGroupMembers(int groupId)
        {
            return _context.ProblemGroupMembers
                .Include(m => m.User)
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.Role)
                .ThenBy(m => m.JoinedAt)
                .ToList();
        }

        public bool UpdateMemberPaymentStatus(int memberId, GroupPaymentStatus status)
        {
            try
            {
                var member = _context.ProblemGroupMembers.Find(memberId);
                if (member == null) return false;

                member.PaymentStatus = status;
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int GetGroupMemberCount(int groupId)
        {
            return _context.ProblemGroupMembers.Count(m => m.GroupId == groupId);
        }

        // ==================== INVITE MANAGEMENT ====================

        public ProblemGroupInvite? CreateInvite(int groupId, int invitedUserId, int invitedByUserId)
        {
            try
            {
                // Check if already invited
                var existing = _context.ProblemGroupInvites
                    .FirstOrDefault(i => i.GroupId == groupId &&
                                        i.InvitedUserId == invitedUserId &&
                                        i.Status == GroupInviteStatus.Pending);

                if (existing != null) return null;

                // Check if already member
                var isMember = _context.ProblemGroupMembers
                    .Any(m => m.GroupId == groupId && m.UserId == invitedUserId);

                if (isMember) return null;

                var invite = new ProblemGroupInvite
                {
                    GroupId = groupId,
                    InvitedUserId = invitedUserId,
                    InvitedByUserId = invitedByUserId,
                    Status = GroupInviteStatus.Pending,
                    InvitedAt = DateTime.Now
                };

                _context.ProblemGroupInvites.Add(invite);
                _context.SaveChanges();

                return invite;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating invite: {ex.Message}");
                return null;
            }
        }

        public bool AcceptInvite(int inviteId, int userId)
        {
            try
            {
                var invite = _context.ProblemGroupInvites
                    .Include(i => i.Group)
                    .FirstOrDefault(i => i.Id == inviteId && i.InvitedUserId == userId);

                if (invite == null || invite.Status != GroupInviteStatus.Pending)
                    return false;

                // Update invite status
                invite.Status = GroupInviteStatus.Accepted;
                invite.RespondedAt = DateTime.Now;

                // Add as member
                AddMember(invite.GroupId, userId, GroupMemberRole.Member);

                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RejectInvite(int inviteId, int userId)
        {
            try
            {
                var invite = _context.ProblemGroupInvites
                    .FirstOrDefault(i => i.Id == inviteId && i.InvitedUserId == userId);

                if (invite == null || invite.Status != GroupInviteStatus.Pending)
                    return false;

                invite.Status = GroupInviteStatus.Rejected;
                invite.RespondedAt = DateTime.Now;

                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<ProblemGroupInvite> GetPendingInvitesForUser(int userId)
        {
            return _context.ProblemGroupInvites
                .Include(i => i.Group)
                    .ThenInclude(g => g.Problem)
                .Include(i => i.InvitedByUser)
                .Where(i => i.InvitedUserId == userId && i.Status == GroupInviteStatus.Pending)
                .OrderByDescending(i => i.InvitedAt)
                .ToList();
        }

        public List<ProblemGroupInvite> GetGroupInvites(int groupId)
        {
            return _context.ProblemGroupInvites
                .Include(i => i.InvitedUser)
                .Where(i => i.GroupId == groupId)
                .OrderByDescending(i => i.InvitedAt)
                .ToList();
        }

        // ==================== PRICE CALCULATION ====================

        public decimal CalculatePricePerMember(int groupId)
        {
            var group = _context.ProblemGroups.Find(groupId);
            if (group == null) return 0;

            var memberCount = GetGroupMemberCount(groupId);
            if (memberCount == 0) return group.TotalPrice;

            return Math.Round(group.TotalPrice / memberCount, 0);
        }

        public bool UpdateGroupPrices(int groupId)
        {
            try
            {
                var group = _context.ProblemGroups.Find(groupId);
                if (group == null) return false;

                group.PricePerMember = CalculatePricePerMember(groupId);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}