using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Domain.Interfaces
{
    public interface IHierarchicalUser
    {
        string Id { get; set; }
        string? SupervisorId { get; set; }
    }
}