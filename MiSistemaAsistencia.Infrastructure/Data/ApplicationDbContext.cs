using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Domain;
using MiSistemaAsistencia.Infrastructure;

namespace MiSistemaAsistencia.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

        public DbSet<Department> Departments { get; set; }
        public DbSet<WorkSchedule> WorkSchedules { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Position> Positions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<LeaveRequest>()
                .HasOne<ApplicationUser>()
                .WithMany(u => u.LeaveRequests) 
                .HasForeignKey(r => r.ApplicationUserId) 
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LeaveRequest>()
                .HasOne<ApplicationUser>()
                .WithMany(u => u.Approvals) 
                .HasForeignKey(r => r.ApprovedByUserId) 
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LeaveRequest>()
                .HasOne(r => (ApplicationUser)r.RequestUser)
                .WithMany(u => u.LeaveRequests)
                .HasForeignKey(r => r.ApplicationUserId)
                .IsRequired();

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.WorkSchedule)
                .WithMany()
                .HasForeignKey(u => u.WorkScheduleId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Position)
                .WithMany()
                .HasForeignKey(u => u.PositionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasOne(empleado => empleado.Supervisor)
                .WithMany()
                .HasForeignKey(empleado => empleado.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
