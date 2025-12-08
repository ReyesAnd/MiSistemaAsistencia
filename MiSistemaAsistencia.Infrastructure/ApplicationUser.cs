using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MiSistemaAsistencia.Domain;
using MiSistemaAsistencia.Domain.Interfaces;

namespace MiSistemaAsistencia.Infrastructure
{
    public class ApplicationUser : IdentityUser, IHierarchicalUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string EmployeeNumber { get; set; }
        public DateTime HireDate { get; set; }
        public int PositionId { get; set; }
        public Position Position { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        public int AvailableVacationDays { get; set; }
        public int? WorkScheduleId { get; set; }
        public WorkSchedule WorkSchedule { get; set; }
        public string? SupervisorId { get; set; }

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; }
        public ICollection<LeaveRequest> LeaveRequests { get; set; }
        public ICollection<LeaveRequest> Approvals { get; set; }
        public virtual ApplicationUser? Supervisor { get; set; }
    }
}
