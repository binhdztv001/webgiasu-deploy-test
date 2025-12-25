using Microsoft.EntityFrameworkCore;

namespace Webgiasu.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
           : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<Solution> Solutions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Message> Messages { get; set; }

        public DbSet<CommunityPost> CommunityPosts { get; set; }
        public DbSet<CommunityComment> CommunityComments { get; set; }

        public DbSet<CommunityPostLike> CommunityPostLikes { get; set; }
        public DbSet<CommunityCommentLike> CommunityCommentLikes { get; set; }

        public DbSet<ProblemGroup> ProblemGroups { get; set; }
        public DbSet<ProblemGroupMember> ProblemGroupMembers { get; set; }
        public DbSet<ProblemGroupInvite> ProblemGroupInvites { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Tutor)
                .WithMany()
                .HasForeignKey(r => r.TutorId)
                .OnDelete(DeleteBehavior.Restrict);   // Tắt cascade delete

            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);   // hoặc giữ cascade, tùy bạn

            // Cấu hình Friendship relationships
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany()
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Addressee)
                .WithMany()
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Đảm bảo không có duplicate friendship (A-B hoặc B-A)
            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.RequesterId, f.AddresseeId })
                .IsUnique();

            // Cấu hình Message relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index cho việc query messages nhanh hơn
            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.SenderId, m.ReceiverId });

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.SentDate);

            // CommunityPost → User
            modelBuilder.Entity<CommunityPost>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // CommunityPostLike → Post
            modelBuilder.Entity<CommunityPostLike>()
                .HasOne(l => l.Post)
                .WithMany()
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // CommunityPostLike → User
            modelBuilder.Entity<CommunityPostLike>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // CommunityCommentLike → Comment
            modelBuilder.Entity<CommunityCommentLike>()
                .HasOne(l => l.Comment)
                .WithMany()
                .HasForeignKey(l => l.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // CommunityCommentLike → User
            modelBuilder.Entity<CommunityCommentLike>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index cho hiệu năng
            modelBuilder.Entity<CommunityPost>()
                .HasIndex(p => p.CreatedAt);

            modelBuilder.Entity<CommunityComment>()
                .HasIndex(c => c.PostId);

            modelBuilder.Entity<CommunityPostLike>()
                .HasIndex(x => new { x.PostId, x.UserId })
                .IsUnique();

            modelBuilder.Entity<CommunityCommentLike>()
                .HasIndex(x => new { x.CommentId, x.UserId })
                .IsUnique();
            // ProblemGroup relationships
            modelBuilder.Entity<ProblemGroup>()
                .HasOne(pg => pg.Problem)
                .WithMany(p => p.Groups)
                .HasForeignKey(pg => pg.ProblemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProblemGroup>()
                .HasOne(pg => pg.CreatedByUser)
                .WithMany(u => u.CreatedGroups)
                .HasForeignKey(pg => pg.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProblemGroupMember relationships
            modelBuilder.Entity<ProblemGroupMember>()
                .HasOne(pgm => pgm.Group)
                .WithMany(pg => pg.Members)
                .HasForeignKey(pgm => pgm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProblemGroupMember>()
                .HasOne(pgm => pgm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(pgm => pgm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProblemGroupInvite relationships
            modelBuilder.Entity<ProblemGroupInvite>()
                .HasOne(pgi => pgi.Group)
                .WithMany(pg => pg.Invites)
                .HasForeignKey(pgi => pgi.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProblemGroupInvite>()
                .HasOne(pgi => pgi.InvitedUser)
                .WithMany(u => u.ReceivedInvites)
                .HasForeignKey(pgi => pgi.InvitedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProblemGroupInvite>()
                .HasOne(pgi => pgi.InvitedByUser)
                .WithMany(u => u.SentInvites)
                .HasForeignKey(pgi => pgi.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for better performance
            modelBuilder.Entity<ProblemGroup>()
                .HasIndex(pg => pg.ProblemId);

            modelBuilder.Entity<ProblemGroupMember>()
                .HasIndex(pgm => new { pgm.GroupId, pgm.UserId })
                .IsUnique();

            modelBuilder.Entity<ProblemGroupInvite>()
                .HasIndex(pgi => new { pgi.GroupId, pgi.InvitedUserId });
        


    }
    }
}
