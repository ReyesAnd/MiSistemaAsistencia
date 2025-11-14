using System.Collections.Generic;

namespace MiSistemaAsistencia.Web.Models
{
    public class AdminDashboardViewModel
    {
        // Tarjetas KPI
        public int TotalEmployees { get; set; }
        public int EmployeesPresent { get; set; }
        public int EmployeesAbsentToday { get; set; }
        public int LateArrivalsToday { get; set; }
        public int PendingApprovalRequests { get; set; }

        // Datos
        public List<RecentActivityViewModel> RecentActivity { get; set; } = new List<RecentActivityViewModel>();
    }

    public class RecentActivityViewModel
    {
        public string EmployeeName { get; set; }
        public string Type { get; set; } 
        public DateTime Time { get; set; }
    }
}