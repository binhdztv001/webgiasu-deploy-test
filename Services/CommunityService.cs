using Microsoft.EntityFrameworkCore;
using Webgiasu.Models;

namespace Webgiasu.Services
{
    public class CommunityService : ICommunityService
    {
        private readonly AppDbContext _context;

        // Valid reaction types
        private static readonly HashSet<string> ValidReactions = new HashSet<string>
        {
            "like", "love", "haha", "angry"
        };

        public CommunityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CommunityComment>> GetCommentsByPostIdAsync(int postId)
        {
            return await _context.CommunityComments
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // Return comments payload used by frontend — author is shown anonymous "Người Ẩn Danh #<UserId>"
        public async Task<List<object>> GetPostCommentsAsync(int postId)
        {
            return await _context.CommunityComments
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.PostId,
                    c.Content,
                    c.IsAnonymous,
                    // Always display anonymous text with user id
                    authorName = $"Người Ẩn Danh #{c.UserId}",
                    // Create an "anonymous" avatar using the user id so different users get distinct images
                    authorAvatar = $"https://ui-avatars.com/api/?name=Anon+{c.UserId}&background=999&color=fff",
                    createdAt = c.CreatedAt.ToString("HH:mm, dd/MM")
                })
                .ToListAsync<object>();
        }

        public async Task<CommunityComment> AddCommentAsync(CommunityComment comment)
        {
            _context.CommunityComments.Add(comment);
            await _context.SaveChangesAsync();

            // load navigation User if needed later (not used for display name as we show anonymous)
            comment.User = await _context.Users.FindAsync(comment.UserId);
            return comment;
        }

        public async Task DeleteCommentAsync(int commentId)
        {
            var comment = await _context.CommunityComments.FindAsync(commentId);
            if (comment != null)
            {
                comment.IsDeleted = true;
                _context.CommunityComments.Update(comment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<CommunityPost>> GetAllPostsAsync()
        {
            return await _context.CommunityPosts
                .Where(p => !p.IsDeleted && !p.IsLocked)
                .Include(p => p.User)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<CommunityPost> CreatePostAsync(CommunityPost post)
        {
            _context.CommunityPosts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<CommunityPost> GetPostByIdAsync(int postId)
        {
            return await _context.CommunityPosts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
        }

        public async Task<CommunityComment?> GetCommentByIdAsync(int commentId)
        {
            return await _context.CommunityComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);
        }

        // ⚠️ DEPRECATED METHODS - Keep for backward compatibility
        public async Task<bool> TogglePostLikeAsync(int postId, int userId)
        {
            var result = await TogglePostReactionAsync(postId, userId, "like");
            return result == "like";
        }

        public async Task<int> GetPostLikeCountAsync(int postId)
        {
            return await _context.CommunityPostLikes.CountAsync(x => x.PostId == postId);
        }

        public async Task<bool> IsPostLikedAsync(int postId, int userId)
        {
            return await _context.CommunityPostLikes.AnyAsync(x => x.PostId == postId && x.UserId == userId);
        }

        public async Task<bool> ToggleCommentLikeAsync(int commentId, int userId)
        {
            var result = await ToggleCommentReactionAsync(commentId, userId, "like");
            return result == "like";
        }

        public async Task<int> GetCommentLikeCountAsync(int commentId)
        {
            return await _context.CommunityCommentLikes.CountAsync(x => x.CommentId == commentId);
        }

        public async Task<bool> IsCommentLikedAsync(int commentId, int userId)
        {
            return await _context.CommunityCommentLikes.AnyAsync(x => x.CommentId == commentId && x.UserId == userId);
        }

        public async Task<bool> UpdatePostAsync(int postId, int userId, string content, string? category)
        {
            var post = await _context.CommunityPosts
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            if (post == null || post.UserId != userId)
                return false;

            post.Content = content;
            post.Category = category;
            // Optional: Add UpdatedAt field if you want to track edits
            // post.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Soft delete post (only by author)
        /// </summary>
        public async Task<bool> DeletePostAsync(int postId, int userId)
        {
            var post = await _context.CommunityPosts
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            if (post == null || post.UserId != userId)
                return false;

            post.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Check if user can edit post (is author)
        /// </summary>
        public async Task<bool> CanEditPostAsync(int postId, int userId)
        {
            var post = await _context.CommunityPosts
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            return post != null && post.UserId == userId;
        }

        // ✨ NEW REACTION SYSTEM
        public async Task<string?> TogglePostReactionAsync(int postId, int userId, string reactionType)
        {
            // Validate reaction type
            reactionType = reactionType?.ToLower();
            if (string.IsNullOrEmpty(reactionType) || !ValidReactions.Contains(reactionType))
            {
                throw new ArgumentException($"Invalid reaction type: {reactionType}");
            }

            var existingReaction = await _context.CommunityPostLikes
                .FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);

            // Case 1: Click cùng reaction -> Remove
            if (existingReaction != null && existingReaction.ReactionType?.ToLower() == reactionType)
            {
                _context.CommunityPostLikes.Remove(existingReaction);
                await _context.SaveChangesAsync();
                return null; // Removed reaction
            }

            // Case 2: Click reaction khác -> Update
            if (existingReaction != null)
            {
                existingReaction.ReactionType = reactionType;
                existingReaction.CreatedAt = DateTime.Now;
                _context.CommunityPostLikes.Update(existingReaction);
                await _context.SaveChangesAsync();
                return reactionType;
            }

            // Case 3: Chưa có reaction -> Insert
            var newReaction = new CommunityPostLike
            {
                PostId = postId,
                UserId = userId,
                ReactionType = reactionType,
                CreatedAt = DateTime.Now
            };

            _context.CommunityPostLikes.Add(newReaction);
            await _context.SaveChangesAsync();
            return reactionType;
        }

        /// <summary>
        /// Lấy thống kê reactions của Post
        /// </summary>
        /// <returns>List { reactionType, count }</returns>
        public async Task<List<object>> GetPostReactionStatsAsync(int postId)
        {
            var stats = await _context.CommunityPostLikes
                .Where(x => x.PostId == postId)
                .GroupBy(x => x.ReactionType.ToLower())
                .Select(g => new
                {
                    reactionType = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .ToListAsync<object>();

            return stats;
        }

        /// <summary>
        /// Lấy reaction hiện tại của user cho Post
        /// </summary>
        public async Task<string?> GetUserPostReactionAsync(int postId, int userId)
        {
            var reaction = await _context.CommunityPostLikes
                .Where(x => x.PostId == postId && x.UserId == userId)
                .Select(x => x.ReactionType)
                .FirstOrDefaultAsync();

            return reaction?.ToLower();
        }

        /// <summary>
        /// Toggle reaction cho Comment
        /// </summary>
        public async Task<string?> ToggleCommentReactionAsync(int commentId, int userId, string reactionType)
        {
            // Validate reaction type
            reactionType = reactionType?.ToLower();
            if (string.IsNullOrEmpty(reactionType) || !ValidReactions.Contains(reactionType))
            {
                throw new ArgumentException($"Invalid reaction type: {reactionType}");
            }

            var existingReaction = await _context.CommunityCommentLikes
                .FirstOrDefaultAsync(x => x.CommentId == commentId && x.UserId == userId);

            // Case 1: Click cùng reaction -> Remove
            if (existingReaction != null && existingReaction.ReactionType?.ToLower() == reactionType)
            {
                _context.CommunityCommentLikes.Remove(existingReaction);
                await _context.SaveChangesAsync();
                return null;
            }

            // Case 2: Click reaction khác -> Update
            if (existingReaction != null)
            {
                existingReaction.ReactionType = reactionType;
                existingReaction.CreatedAt = DateTime.Now;
                _context.CommunityCommentLikes.Update(existingReaction);
                await _context.SaveChangesAsync();
                return reactionType;
            }

            // Case 3: Chưa có reaction -> Insert
            var newReaction = new CommunityCommentLike
            {
                CommentId = commentId,
                UserId = userId,
                ReactionType = reactionType,
                CreatedAt = DateTime.Now
            };

            _context.CommunityCommentLikes.Add(newReaction);
            await _context.SaveChangesAsync();
            return reactionType;
        }

        /// <summary>
        /// Lấy thống kê reactions của Comment
        /// </summary>
        public async Task<List<object>> GetCommentReactionStatsAsync(int commentId)
        {
            var stats = await _context.CommunityCommentLikes
                .Where(x => x.CommentId == commentId)
                .GroupBy(x => x.ReactionType.ToLower())
                .Select(g => new
                {
                    reactionType = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .ToListAsync<object>();

            return stats;
        }

        /// <summary>
        /// Lấy reaction hiện tại của user cho Comment
        /// </summary>
        public async Task<string?> GetUserCommentReactionAsync(int commentId, int userId)
        {
            var reaction = await _context.CommunityCommentLikes
                .Where(x => x.CommentId == commentId && x.UserId == userId)
                .Select(x => x.ReactionType)
                .FirstOrDefaultAsync();

            return reaction?.ToLower();
        }
    }
}