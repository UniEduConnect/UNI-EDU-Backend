using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<ClassSession> ClassSessions { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<Submission> Submissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configure Composite Key for ExamQuestion
            modelBuilder.Entity<ExamQuestion>()
                .HasKey(eq => new { eq.ExamID, eq.QuestionID });

            // 2. Configure 1-to-1 relationships explicitly
            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.User)
                .WithOne(u => u.Tutor)
                .HasForeignKey<Tutor>(t => t.TutorID);

            modelBuilder.Entity<Parent>()
                .HasOne(p => p.User)
                .WithOne(u => u.Parent)
                .HasForeignKey<Parent>(p => p.ParentID);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.StudentID);

            // 3. Prevent Cascade Delete Cycles (Multiple Cascade Paths)

            // Restrict ClassSession deletes
            modelBuilder.Entity<ClassSession>()
                .HasOne(c => c.Tutor)
                .WithMany(t => t.Classes)
                .HasForeignKey(c => c.TutorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassSession>()
                .HasOne(c => c.Student)
                .WithMany(s => s.Classes)
                .HasForeignKey(c => c.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict Review deletes
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Tutor)
                .WithMany()
                .HasForeignKey(r => r.TutorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.ClassSession)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.ClassID)
                .OnDelete(DeleteBehavior.Cascade); // Deleting class removes its reviews

            // Restrict Submission deletes
            modelBuilder.Entity<Submission>()
                .HasOne(s => s.User)
                .WithMany(u => u.Submissions)
                .HasForeignKey(s => s.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Exam)
                .WithMany(e => e.Submissions)
                .HasForeignKey(s => s.ExamID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}