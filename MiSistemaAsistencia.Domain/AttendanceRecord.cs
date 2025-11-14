using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Domain
{
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public double OvertimeHours { get; set; }
    }
}
