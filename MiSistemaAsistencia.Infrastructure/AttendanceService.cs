using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Application;
using MiSistemaAsistencia.Domain;
using MiSistemaAsistencia.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Infrastructure
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;
        public AttendanceService(ApplicationDbContext context) { _context = context; }

        public async Task<bool> ClockIn(string userId)
        {
            var hasOpenRecord = await _context.AttendanceRecords
                .AnyAsync(a => a.ApplicationUserId == userId && a.CheckOutTime == null);

            if (hasOpenRecord)
            {
                return false;
            }

            var record = new AttendanceRecord
            {
                ApplicationUserId = userId,
                CheckInTime = DateTime.Now,
                CheckOutTime = null
            };

            _context.AttendanceRecords.Add(record);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClockOut(string userId)
        {
            var record = await _context.AttendanceRecords
                .Where(a =>
                    a.ApplicationUserId == userId &&
                    a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return false;
            }

            record.CheckOutTime = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
