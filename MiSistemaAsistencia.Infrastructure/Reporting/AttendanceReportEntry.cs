using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Infrastructure.Reporting
{
    public class AttendanceReportEntry
    {
        public string EmployeeNumber { get; set; }
        public string FullName { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
    }
}
