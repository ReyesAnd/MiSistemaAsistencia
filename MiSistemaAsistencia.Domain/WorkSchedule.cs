using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Domain
{
    public class WorkSchedule
    {
        public int Id { get; set; }
        public string Name { get; set; } 
        public TimeSpan ExpectedCheckIn { get; set; }
        public TimeSpan ExpectedCheckOut { get; set; }
       
    }
}
