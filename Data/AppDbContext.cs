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

        // Cấu hình mối quan hệ 1 Sinh viên - Nhiều lịch hẹn hiến máu
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.User)
            .WithMany(u => u.Appointments)
            .HasForeignKey(a => a.UserId);

        // Cấu hình mối quan hệ 1 Sự kiện chiến dịch - Nhiều lịch hẹn
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.BloodEvent)
            .WithMany(e => e.Appointments)
            .HasForeignKey(a => a.BloodEventId);
    }
}