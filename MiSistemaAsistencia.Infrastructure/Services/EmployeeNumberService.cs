using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Application;
using MiSistemaAsistencia.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Infrastructure.Services
{
    public class EmployeeNumberService : IEmployeeNumberService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeNumberService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetNextEmployeeNumberAsync()
        {
            var lastEmployeeNumberStr = await _context.Users
                .OrderByDescending(u => u.EmployeeNumber)
                .Select(u => u.EmployeeNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastEmployeeNumberStr))
            {
                if (int.TryParse(lastEmployeeNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return nextNumber.ToString("D3");
        }
    }
}