using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Application
{
    public interface IAttendanceService
    {
        Task<bool> ClockIn(string userId);
        Task<bool> ClockOut(string userId);
    }
}
