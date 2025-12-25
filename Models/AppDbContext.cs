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
        }



    }
}
