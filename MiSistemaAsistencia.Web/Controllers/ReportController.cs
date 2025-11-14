using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Infrastructure.Data;
using MiSistemaAsistencia.Infrastructure.Reporting;
using OfficeOpenXml; 
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;

namespace MiSistemaAsistencia.Web.Controllers
{
    [Authorize(Roles = "Supervisor, Administrador")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Report
        public IActionResult Index()
        {
            return View();
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