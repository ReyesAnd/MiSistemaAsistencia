using Microsoft.AspNetCore.Mvc;
using System;

namespace MiSistemaAsistencia.Web.Models
{
    public class EmployeeDashboardViewModel
    {
        // Tarjeta de Estado Actual
        public string CurrentStatus { get; set; }
        public DateTime? LastCheckInTime { get; set; }

        // Tarjeta de Vacaciones
        public int AvailableVacationDays { get; set; }

        // Tarjeta de Horas (Semana)
        public double HoursWorkedThisWeek { get; set; }

        // Tarjeta de Solicitudes
        public int PendingLeaveRequests { get; set; }

        public bool IsCurrentlyCheckedIn { get; set; }
    }
}
