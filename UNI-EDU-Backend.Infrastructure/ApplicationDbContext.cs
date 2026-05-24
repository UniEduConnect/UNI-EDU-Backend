using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
        public DbSet<Class> Classes { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<TutorSubject> TutorSubjects { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Session> Sessions { get; set; }

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

            // Restrict Class deletes
            modelBuilder.Entity<Class>()
                .HasOne(c => c.Tutor)
                .WithMany(t => t.Classes)
                .HasForeignKey(c => c.TutorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Class>()
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
                .HasOne(r => r.Class)
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

            // Tutor <-> Subject M2M via TutorSubject join entity
            modelBuilder.Entity<TutorSubject>()
                .HasKey(ts => new { ts.TutorID, ts.SubjectID });

            modelBuilder.Entity<TutorSubject>()
                .HasOne(ts => ts.Tutor)
                .WithMany(t => t.TutorSubjects)
                .HasForeignKey(ts => ts.TutorID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TutorSubject>()
                .HasOne(ts => ts.Subject)
                .WithMany()
                .HasForeignKey(ts => ts.SubjectID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tutor>()
                .HasMany(t => t.Subjects)
                .WithMany(s => s.Tutors)
                .UsingEntity<TutorSubject>(
                    j => j.HasOne(ts => ts.Subject).WithMany().HasForeignKey(ts => ts.SubjectID),
                    j => j.HasOne(ts => ts.Tutor).WithMany(t => t.TutorSubjects).HasForeignKey(ts => ts.TutorID));

            // Wallet 1-to-1 with User (UserID is PK + FK)
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithOne()
                .HasForeignKey<Wallet>(w => w.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(t => t.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(t => t.Class)
                .WithMany()
                .HasForeignKey(t => t.RelatedClassID)
                .OnDelete(DeleteBehavior.SetNull);

            // Session child of Class (cascade)
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Sessions)
                .HasForeignKey(s => s.ClassID)
                .OnDelete(DeleteBehavior.Cascade);

            // Postgres array / jsonb columns on Tutor
            modelBuilder.Entity<Tutor>()
                .Property(t => t.Certificates)
                .HasColumnType("text[]");

            modelBuilder.Entity<Tutor>()
                .Property(t => t.Achievements)
                .HasColumnType("text[]");

            modelBuilder.Entity<Tutor>()
                .Property(t => t.AvailableSlots)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<AvailableSlot>>(v, (JsonSerializerOptions?)null) ?? new List<AvailableSlot>());

            modelBuilder.Entity<Class>()
                .Property(c => c.WeeklySlots)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<ClassScheduleSlot>>(v, (JsonSerializerOptions?)null) ?? new List<ClassScheduleSlot>());
        }
    }
}