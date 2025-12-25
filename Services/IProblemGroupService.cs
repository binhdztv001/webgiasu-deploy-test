using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface IProblemGroupService
    {
        // Group Management
        ProblemGroup? CreateProblemGroup(int problemId, int createdByUserId, string? groupName, decimal totalPrice);
        ProblemGroup? GetGroupById(int groupId);
        ProblemGroup? GetGroupByProblemId(int problemId);
        List<ProblemGroup> GetGroupsByUserId(int userId);
        bool UpdateGroupStatus(int groupId, ProblemGroupStatus status);
        bool DeleteGroup(int groupId);

        // Member Management
        bool AddMember(int groupId, int userId, GroupMemberRole role);
        bool RemoveMember(int groupId, int userId);
        List<ProblemGroupMember> GetGroupMembers(int groupId);
        bool UpdateMemberPaymentStatus(int memberId, GroupPaymentStatus status);
        int GetGroupMemberCount(int groupId);

        // Invite Management
        ProblemGroupInvite? CreateInvite(int groupId, int invitedUserId, int invitedByUserId);
        bool AcceptInvite(int inviteId, int userId);
        bool RejectInvite(int inviteId, int userId);
        List<ProblemGroupInvite> GetPendingInvitesForUser(int userId);
        List<ProblemGroupInvite> GetGroupInvites(int groupId);

        // Price Calculation
        decimal CalculatePricePerMember(int groupId);
        bool UpdateGroupPrices(int groupId);
    }
}