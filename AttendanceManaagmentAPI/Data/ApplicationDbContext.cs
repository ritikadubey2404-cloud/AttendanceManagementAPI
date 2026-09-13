using AttendanceManaagmentAPI.Models;
using AttendanceManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManaagmentAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =====================================================
        // DATABASE TABLES
        // =====================================================

        public DbSet<User> Users { get; set; }

        public DbSet<Student> Students { get; set; }

        public DbSet<Teacher> Teachers { get; set; }

        public DbSet<Admin> Admin { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<LeaveApplication> LeaveApplications { get; set; }

        public DbSet<Notification> Notifications { get; set; }


        // =====================================================
        // TABLE MAPPING
        // =====================================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =================================================
            // TABLE NAMES
            // =================================================

            modelBuilder.Entity<User>()
                .ToTable("users");

            modelBuilder.Entity<Student>()
                .ToTable("students");

            modelBuilder.Entity<Teacher>()
                .ToTable("teachers");

            modelBuilder.Entity<Admin>()
                .ToTable("admin");

            modelBuilder.Entity<Attendance>()
                .ToTable("attendance");

            modelBuilder.Entity<LeaveApplication>()
                .ToTable("leaveapplication");

            modelBuilder.Entity<Notification>()
                .ToTable("notification");


            // =================================================
            // NOTIFICATION CONFIGURATION
            // =================================================

            modelBuilder.Entity<Notification>()
                .HasKey(n => n.NotificationId);

            modelBuilder.Entity<Notification>()
                .Property(n => n.StudentId)
                .IsRequired();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Message)
                .IsRequired();

            modelBuilder.Entity<Notification>()
                .Property(n => n.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            modelBuilder.Entity<Notification>()
                .Property(n => n.SentOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");


            // =================================================
            // LEAVE APPLICATION CONFIGURATION
            // =================================================

            modelBuilder.Entity<LeaveApplication>()
                .HasKey(l => l.LeaveId);

            modelBuilder.Entity<LeaveApplication>()
                .Property(l => l.StudentId)
                .IsRequired();

            modelBuilder.Entity<LeaveApplication>()
                .Property(l => l.TeacherId)
                .IsRequired();

            modelBuilder.Entity<LeaveApplication>()
                .Property(l => l.LeaveDate)
                .IsRequired();

            modelBuilder.Entity<LeaveApplication>()
                .Property(l => l.Reason)
                .IsRequired();

            modelBuilder.Entity<LeaveApplication>()
                .Property(l => l.Status)
                .IsRequired()
                .HasDefaultValue("Pending");

            modelBuilder.Entity<LeaveApplication>()
                .Property(l => l.TeacherRemark)
                .HasMaxLength(255);

            modelBuilder.Entity<LeaveApplication>()
                .Property(l => l.AppliedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}