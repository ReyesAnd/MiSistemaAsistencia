using MiSistemaAsistencia.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Domain
{
    public class LeaveRequest
    {
        public int Id { get; set; }
        public string? ApplicationUserId { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public LeaveType Type { get; set; } // Enum
        public LeaveStatus Status { get; set; } // Enum

        public string? ApprovedByUserId { get; set; } // Supervisor/Admin
        public string? RejectionReason { get; set; }
        public virtual IHierarchicalUser? RequestUser { get; set; }
    }

    public enum LeaveType { Vacaciones, Permisos }
    public enum LeaveStatus { Pending, Approved, Rejected }
}
