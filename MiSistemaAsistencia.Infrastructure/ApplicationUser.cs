using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MiSistemaAsistencia.Domain;

namespace MiSistemaAsistencia.Infrastructure
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmployeeNumber { get; set; }
        public DateTime HireDate { get; set; }
        public int PositionId { get; set; }
        public Position Position { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        public int AvailableVacationDays { get; set; }
        public int? WorkScheduleId { get; set; }
        public WorkSchedule WorkSchedule { get; set; }

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; }
        public ICollection<LeaveRequest> LeaveRequests { get; set; }
        public ICollection<LeaveRequest> Approvals { get; set; }
    }
}
