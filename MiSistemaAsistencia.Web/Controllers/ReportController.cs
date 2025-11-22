using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Infrastructure.Data;
using MiSistemaAsistencia.Infrastructure.Reporting;
using OfficeOpenXml; 
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using MiSistemaAsistencia.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Web.Controllers
{
    [Authorize(Roles = "Supervisor, Administrador")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<MiSistemaAsistencia.Infrastructure.ApplicationUser> _userManager;

        public ReportController(ApplicationDbContext context, Microsoft.AspNetCore.Identity.UserManager<Infrastructure.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Report
        public IActionResult Index()
        {
            return View(new ReportViewModel { StartDate = DateTime.Today, EndDate = DateTime.Today });
        }

        // POST: /Report/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ReportViewModel model)
        {
            model.Results = new List<ReportItem>();

            var filterEnd = model.EndDate.Date.AddDays(1).AddTicks(-1);
            var filterStart = model.StartDate.Date;

            // REPORTES DE ASISTENCIA, PRESENTES Y TARDANZAS
            if (model.ReportType != "Ausentes")
            {
                var query = from r in _context.AttendanceRecords
                            join u in _context.Users on r.ApplicationUserId equals u.Id
                            join s in _context.WorkSchedules on u.WorkScheduleId equals s.Id into schedules
                            from sched in schedules.DefaultIfEmpty()
                            join d in _context.Departments on u.DepartmentId equals d.Id into departments
                            from dept in departments.DefaultIfEmpty()
                            where r.CheckInTime >= filterStart && r.CheckInTime <= filterEnd
                            select new
                            {
                                Record = r,
                                User = u,
                                Schedule = sched,
                                Department = dept
                            };

                if (model.ReportType == "Tardanzas")
                {
                    query = query.Where(x => x.Schedule != null &&
                                             x.Record.CheckInTime.TimeOfDay > x.Schedule.ExpectedCheckIn);
                }

                var rawResults = await query.OrderByDescending(x => x.Record.CheckInTime).ToListAsync();

                model.Results = rawResults.Select(x => new ReportItem
                {
                    EmployeeName = x.User.FirstName + " " + x.User.LastName,
                    EmployeeNumber = x.User.EmployeeNumber,
                    Department = x.Department?.Name ?? "N/A",
                    Date = x.Record.CheckInTime.Date,
                    CheckIn = x.Record.CheckInTime,
                    CheckOut = x.Record.CheckOutTime,
                    ExpectedTime = x.Schedule?.ExpectedCheckIn,
                    Status = model.ReportType == "Tardanzas" ? "Tardanza" : "Presente"
                }).ToList();
            }

            // ---------------------------------------------------------
            // REPORTE DE AUSENCIAS
            else if (model.ReportType == "Ausentes")
            {
                var activeUsers = await _userManager.Users
                    .Include(u => u.Department)
                    .Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow)
                    .ToListAsync();

                var attendanceInRange = await _context.AttendanceRecords
                    .Where(r => r.CheckInTime >= filterStart && r.CheckInTime <= filterEnd)
                    .Select(r => new { r.ApplicationUserId, r.CheckInTime })
                    .ToListAsync();

                for (var day = filterStart; day <= model.EndDate.Date; day = day.AddDays(1))
                {
                    if (day.DayOfWeek == DayOfWeek.Sunday) continue;

                    foreach (var user in activeUsers)
                    {
                        bool attended = attendanceInRange.Any(r => r.ApplicationUserId == user.Id && r.CheckInTime.Date == day);

                        if (!attended)
                        {
                            model.Results.Add(new ReportItem
                            {
                                EmployeeName = user.FirstName + " " + user.LastName,
                                EmployeeNumber = user.EmployeeNumber,
                                Department = user.Department?.Name ?? "N/A",
                                Date = day,
                                Status = "Ausente",
                                Comments = "Sin registro de entrada"
                            });
                        }
                    }
                }
                model.Results = model.Results.OrderBy(r => r.Date).ThenBy(r => r.EmployeeName).ToList();
            }

            return View(model);
        }

        // POST: /Report/ExportToExcel
        [HttpPost]
        public async Task<IActionResult> ExportToExcel(DateTime startDate, DateTime endDate)
        {
            var reportData = await _context.AttendanceRecords
                .Where(a => a.CheckInTime.Date >= startDate.Date && a.CheckInTime.Date <= endDate.Date)
                .Join(_context.Users,
                    record => record.ApplicationUserId,
                    user => user.Id,
                    (record, user) => new AttendanceReportEntry
                    {
                        EmployeeNumber = user.EmployeeNumber,
                        FullName = user.FirstName + " " + user.LastName,
                        CheckInTime = record.CheckInTime,
                        CheckOutTime = record.CheckOutTime
                    })
                .OrderBy(r => r.CheckInTime) 
                .ThenBy(r => r.CheckInTime)
                .ToListAsync();


            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte de Asistencia");

                // --- Cabeceras ---
                worksheet.Cells["A1"].Value = "EmpleadoId";
                worksheet.Cells["B1"].Value = "Empleado";
                worksheet.Cells["C1"].Value = "Fecha";
                worksheet.Cells["D1"].Value = "Hora de Entrada";
                worksheet.Cells["E1"].Value = "Hora de Salida";
                worksheet.Cells["F1"].Value = "Horas Totales";
                worksheet.Cells["A1:F1"].Style.Font.Bold = true;

                // --- Llenar Datos ---
                int row = 2;
                foreach (var record in reportData)
                {
                    var totalHours = record.CheckOutTime.HasValue ?
                        (record.CheckOutTime.Value - record.CheckInTime).TotalHours : 0;

                    worksheet.Cells[row, 1].Value = record.EmployeeNumber;
                    worksheet.Cells[row, 2].Value = record.FullName;
                    worksheet.Cells[row, 3].Value = record.CheckInTime.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 4].Value = record.CheckInTime.ToString("hh:mm tt");
                    worksheet.Cells[row, 5].Value = record.CheckOutTime.HasValue ? record.CheckOutTime.Value.ToString("hh:mm tt") : "N/A";
                    worksheet.Cells[row, 6].Value = totalHours.ToString("F2");
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                // Devolver el archivo
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream);
                stream.Position = 0;

                string excelName = $"Reporte_Asistencia_{startDate:yyyy-MM-dd}_al_{endDate:yyyy-MM-dd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        // POST: /Report/ExportToPdf
        [HttpPost]
        public async Task<IActionResult> ExportToPdf(DateTime startDate, DateTime endDate)
        {
            var reportData = await _context.AttendanceRecords
                .Where(a => a.CheckInTime.Date >= startDate.Date && a.CheckInTime.Date <= endDate.Date)
                .Join(_context.Users,
                    record => record.ApplicationUserId,
                    user => user.Id,
                    (record, user) => new AttendanceReportEntry
                    {
                        EmployeeNumber = user.EmployeeNumber,
                        FullName = user.FirstName + " " + user.LastName,
                        CheckInTime = record.CheckInTime,
                        CheckOutTime = record.CheckOutTime
                    })
                .OrderBy(r => r.CheckInTime)
                .ThenBy(r => r.CheckInTime)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;
            var report = new AttendanceReport(reportData, startDate, endDate);

            byte[] pdfBytes = report.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Reporte_Asistencia_{startDate:yyyy-MM-dd}.pdf");
        }
    }
}