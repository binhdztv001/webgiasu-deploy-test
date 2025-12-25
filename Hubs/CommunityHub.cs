using Microsoft.AspNetCore.SignalR;
using Webgiasu.Models;
using Webgiasu.Services;

namespace Webgiasu.Hubs
{
    public class CommunityHub : Hub
    {
        private readonly ICommunityService _communityService;

        public CommunityHub(ICommunityService communityService)
        {
            _communityService = communityService;
        }

        // Người dùng join vào post cụ thể
        public async Task JoinPost(int postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"post-{postId}");
        }

        // Gửi bình luận mới
        public async Task SendComment(int postId, int userId, string content, bool isAnonymous)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            var comment = new CommunityComment
            {
                PostId = postId,
                UserId = userId,
                Content = content,
                IsAnonymous = isAnonymous,
                CreatedAt = DateTime.Now
            };

            var created = await _communityService.AddCommentAsync(comment);

            var authorName = created.IsAnonymous ? $"Người Ẩn Danh #{created.UserId}" : (created.User?.FullName ?? $"Người Ẩn Danh #{created.UserId}");
            var authorAvatar = created.IsAnonymous
                ? $"https://ui-avatars.com/api/?name=Anon+{created.UserId}&background=999&color=fff"
                : $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(created.User?.FullName ?? "User")}&background=667eea&color=fff";

            var payload = new
            {
                id = created.Id,
                postId = created.PostId,
                userId = created.UserId,
                content = created.Content,
                isAnonymous = created.IsAnonymous,
                authorName,
                authorAvatar,
                createdAt = created.CreatedAt.ToString("HH:mm, dd/MM")
            };

            await Clients.Group($"post-{postId}").SendAsync("CommentAdded", payload);
        }

        // Xóa bình luận
        public async Task DeleteComment(int commentId, int userId)
        {
            var comment = await _communityService.GetCommentByIdAsync(commentId);
            if (comment == null) return;
            if (comment.UserId != userId) return;

            await _communityService.DeleteCommentAsync(commentId);

            await Clients.Group($"post-{comment.PostId}")
                .SendAsync("CommentDeleted", commentId);
        }

        // Typing notification
        public async Task Typing(int postId, int userId, string userName, bool isTyping)
        {
            await Clients.Group($"post-{postId}").SendAsync("UserTyping", new
            {
                postId,
                userId,
                userName,
                isTyping
            });
        }

        // ✨ NEW: React to Post
        /// <summary>
        /// React to a post with specified reaction type
        /// Valid reactions: like, love, haha, angry
        /// </summary>
        public async Task<object> ReactPost(int postId, int userId, string reactionType)
        {
            try
            {
                // Toggle reaction (remove if same, update if different, insert if new)
                var userReaction = await _communityService.TogglePostReactionAsync(postId, userId, reactionType);

                // Get updated reaction statistics
                var reactionStats = await _communityService.GetPostReactionStatsAsync(postId);

                var payload = new
                {
                    postId,
                    userId,
                    userReaction, // null if removed, otherwise reaction type
                    reactionStats // [{ reactionType, count }, ...]
                };

                // Broadcast to all clients viewing this post
                await Clients.Group($"post-{postId}").SendAsync("PostReactionUpdated", payload);

                return payload;
            }
            catch (ArgumentException ex)
            {
                // Invalid reaction type
                throw new HubException(ex.Message);
            }
        }

        // ✨ NEW: React to Comment
        /// <summary>
        /// React to a comment with specified reaction type
        /// Valid reactions: like, love, haha, angry
        /// </summary>
        public async Task<object> ReactComment(int commentId, int userId, string reactionType)
        {
            try
            {
                // Toggle reaction
                var userReaction = await _communityService.ToggleCommentReactionAsync(commentId, userId, reactionType);

                // Get updated reaction statistics
                var reactionStats = await _communityService.GetCommentReactionStatsAsync(commentId);

                // Get comment to know which post group to broadcast to
                var comment = await _communityService.GetCommentByIdAsync(commentId);
                var postId = comment?.PostId ?? 0;

                var payload = new
                {
                    commentId,
                    postId,
                    userId,
                    userReaction,
                    reactionStats
                };

                if (postId > 0)
                {
                    await Clients.Group($"post-{postId}").SendAsync("CommentReactionUpdated", payload);
                }

                return payload;
            }
            catch (ArgumentException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        // ⚠️ DEPRECATED - Keep for backward compatibility
        public async Task<object> TogglePostLike(int postId, int userId)
        {
            return await ReactPost(postId, userId, "like");
        }

        // ⚠️ DEPRECATED - Keep for backward compatibility
        public async Task<object> ToggleCommentLike(int commentId, int userId)
        {
            return await ReactComment(commentId, userId, "like");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<object> UpdatePost(int postId, int userId, string content, string? category)
        {
            try
            {
                var success = await _communityService.UpdatePostAsync(postId, userId, content, category);

                if (!success)
                {
                    throw new HubException("Không thể cập nhật bài viết. Bạn không có quyền hoặc bài viết không tồn tại.");
                }

                var payload = new
                {
                    postId,
                    content,
                    category,
                    updatedAt = DateTime.Now.ToString("HH:mm, dd/MM")
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("PostUpdated", payload);

                return new { success = true, message = "Cập nhật thành công", data = payload };
            }
            catch (Exception ex)
            {
                throw new HubException($"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete post via SignalR
        /// </summary>
        public async Task<object> DeletePost(int postId, int userId)
        {
            try
            {
                var success = await _communityService.DeletePostAsync(postId, userId);

                if (!success)
                {
                    throw new HubException("Không thể xóa bài viết. Bạn không có quyền hoặc bài viết không tồn tại.");
                }

                var payload = new { postId };

                // Broadcast to all clients
                await Clients.All.SendAsync("PostDeleted", payload);

                return new { success = true, message = "Xóa bài viết thành công" };
            }
            catch (Exception ex)
            {
                throw new HubException($"Lỗi: {ex.Message}");
            }
        }
    }
}