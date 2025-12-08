using Microsoft.AspNetCore.Mvc;
using MiSistemaAsistencia.Infrastructure.Helpers;
using System;
using System.Collections.Generic;

namespace MiSistemaAsistencia.Web.ViewModels
{
    public class ReportViewModel
    {
        public DateTime StartDate { get; set; } = TimeZoneHelper.GetRDNow();
        public DateTime EndDate { get; set; } = TimeZoneHelper.GetRDNow();
        public string ReportType { get; set; } // "Asistencia", "Presentes", "Ausentes", "Tardanzas"
        
        public List<ReportItem> Results { get; set; } = new List<ReportItem>();
    }

    public class ReportItem
    {
        public string EmployeeName { get; set; }
        public string EmployeeNumber { get; set; }
        public string Department { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } // "Presente", "Ausente", "Tarde"

        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public TimeSpan? ExpectedTime { get; set; }
        public string Comments { get; set; }
    }
}
