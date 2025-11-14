using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Application
{
    public interface IEmployeeNumberService
    {
        Task<string> GetNextEmployeeNumberAsync();
    }
}
