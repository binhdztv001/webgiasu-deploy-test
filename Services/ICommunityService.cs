using Webgiasu.Models;

namespace Webgiasu.Services
{
    public interface ICommunityService
    {
        Task<List<CommunityComment>> GetCommentsByPostIdAsync(int postId);
        Task<List<object>> GetPostCommentsAsync(int postId);
        Task<CommunityComment> AddCommentAsync(CommunityComment comment);
        Task DeleteCommentAsync(int commentId);
        Task<List<CommunityPost>> GetAllPostsAsync();
        Task<CommunityPost> CreatePostAsync(CommunityPost post);
        Task<CommunityPost> GetPostByIdAsync(int postId);
        Task<CommunityComment?> GetCommentByIdAsync(int commentId);
        Task<bool> UpdatePostAsync(int postId, int userId, string content, string? category);
        Task<bool> DeletePostAsync(int postId, int userId);
        Task<bool> CanEditPostAsync(int postId, int userId);

        // POST LIKE
        Task<bool> TogglePostLikeAsync(int postId, int userId);
        Task<int> GetPostLikeCountAsync(int postId);
        Task<bool> IsPostLikedAsync(int postId, int userId);

        // COMMENT LIKE
        Task<bool> ToggleCommentLikeAsync(int commentId, int userId);
        Task<int> GetCommentLikeCountAsync(int commentId);
        Task<bool> IsCommentLikedAsync(int commentId, int userId);

        
        // Post Reactions
        Task<string?> TogglePostReactionAsync(int postId, int userId, string reactionType);
        Task<List<object>> GetPostReactionStatsAsync(int postId);
        Task<string?> GetUserPostReactionAsync(int postId, int userId);

        // Comment Reactions
        Task<string?> ToggleCommentReactionAsync(int commentId, int userId, string reactionType);
        Task<List<object>> GetCommentReactionStatsAsync(int commentId);
        Task<string?> GetUserCommentReactionAsync(int commentId, int userId);
    }
}
