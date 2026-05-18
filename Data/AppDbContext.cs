using Microsoft.EntityFrameworkCore;
using VLU.BloodDonation.Api.Data.Entities;

namespace VLU.BloodDonation.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<BloodEvent> BloodEvents { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<BloodStock> BloodStocks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Cấu hình các mối quan hệ (Relationships)
        // Mối quan hệ 1 Sinh viên - Nhiều lịch hẹn hiến máu
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.User)
            .WithMany(u => u.Appointments)
            .HasForeignKey(a => a.UserId);

        // Mối quan hệ 1 Sự kiện chiến dịch - Nhiều lịch hẹn
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.BloodEvent)
            .WithMany(e => e.Appointments)
            .HasForeignKey(a => a.BloodEventId);


        // 2. Seed Data: tài khoản Admin gốc
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 999999,
                StudentId = "ADMIN-GOC",
                FullName = "Quản Trị Viên Hệ Thống",
                Email = "admin.vlu@vanlanguni.vn",
                PasswordHash = "admin_secure_password_hash",
                Role = "Admin",
                IsApproved = true,
                TotalPoints = 0,
                DateCreated = new DateTime(2026, 1, 1)
            }
        );
    }
}