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
        public DbSet<ClassMaterial> ClassMaterials { get; set; }
        public DbSet<Withdrawal> Withdrawals { get; set; }
        public DbSet<TrialBooking> TrialBookings { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<DmMessage> DmMessages { get; set; }
        public DbSet<ClassChatRead> ClassChatReads { get; set; }

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

            // ClassMaterial child of Class (cascade)
            modelBuilder.Entity<ClassMaterial>()
                .HasOne(m => m.Class)
                .WithMany(c => c.Materials)
                .HasForeignKey(m => m.ClassID)
                .OnDelete(DeleteBehavior.Cascade);

            // TrialBooking FKs: Restrict on Tutor/Student/Subject (same as Class — these are user-facing
            // aggregates), SetNull on Parent so deleting the Parent user doesn't take trial history with it.
            modelBuilder.Entity<TrialBooking>()
                .HasOne(t => t.Tutor)
                .WithMany()
                .HasForeignKey(t => t.TutorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TrialBooking>()
                .HasOne(t => t.Student)
                .WithMany()
                .HasForeignKey(t => t.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TrialBooking>()
                .HasOne(t => t.Parent)
                .WithMany()
                .HasForeignKey(t => t.ParentID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TrialBooking>()
                .HasOne(t => t.Subject)
                .WithMany()
                .HasForeignKey(t => t.SubjectID)
                .OnDelete(DeleteBehavior.Restrict);

            // Withdrawal FKs: keep history if tutor deleted; null out reviewer link if their user is deleted.
            modelBuilder.Entity<Withdrawal>()
                .HasOne(w => w.Tutor)
                .WithMany()
                .HasForeignKey(w => w.TutorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Withdrawal>()
                .HasOne(w => w.Reviewer)
                .WithMany()
                .HasForeignKey(w => w.ReviewerID)
                .OnDelete(DeleteBehavior.SetNull);

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
                    v => JsonSerializer.Serialize(v ?? new List<AvailableSlot>(), JsonbOptions),
                    v => DeserializeJsonList<AvailableSlot>(v));

            modelBuilder.Entity<Class>()
                .Property(c => c.WeeklySlots)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v ?? new List<ClassScheduleSlot>(), JsonbOptions),
                    v => DeserializeJsonList<ClassScheduleSlot>(v));

            // Homework attachment URLs captured when a tutor ends a session (nullable text[]).
            modelBuilder.Entity<Session>()
                .Property(s => s.HomeworkFiles)
                .HasColumnType("text[]");

            // Class chat: message cascades with the class; the sender link is Restrict so a
            // chat message never deletes a user.
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Class)
                .WithMany()
                .HasForeignKey(m => m.ClassID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => new { m.ClassID, m.SentAt });

            // Parent ↔ tutor DM: plain Guid columns, indexed by conversation pair for lookup.
            modelBuilder.Entity<DmMessage>()
                .HasIndex(d => new { d.TutorID, d.ParentID, d.SentAt });

            // Per-(class, user) read marker for the class chat.
            modelBuilder.Entity<ClassChatRead>()
                .HasKey(r => new { r.ClassID, r.UserID });
        }

        // Shared options for jsonb columns. camelCase naming matches the on-disk shape
        // already in the DB (e.g. {"day":"Mon","time":"18:00-20:00"}); case-insensitive
        // matching means we still read rows that were written with PascalCase keys.
        private static readonly JsonSerializerOptions JsonbOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Defensive: tolerate empty/null/malformed jsonb values (e.g. '""', 'null', NULL) by
        // returning an empty list instead of throwing during materialization.
        private static List<T> DeserializeJsonList<T>(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<T>();
            try
            {
                return JsonSerializer.Deserialize<List<T>>(raw, JsonbOptions) ?? new List<T>();
            }
            catch (JsonException)
            {
                return new List<T>();
            }
        }
    }
}