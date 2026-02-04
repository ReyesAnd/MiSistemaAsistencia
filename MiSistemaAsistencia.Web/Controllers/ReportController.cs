using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Domain;
using MiSistemaAsistencia.Infrastructure.Data;
using MiSistemaAsistencia.Infrastructure.Helpers;
using MiSistemaAsistencia.Infrastructure.Reporting;
using MiSistemaAsistencia.Web.ViewModels;
using OfficeOpenXml; 
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<MiSistemaAsistencia.Infrastructure.ApplicationUser> _userManager;

        public ReportController(ApplicationDbContext context, Microsoft.AspNetCore.Identity.UserManager<Infrastructure.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Report/Index
        [HttpGet]
        public async Task<IActionResult> Index(string reportType, DateTime? date)
        {
            var targetDate = date ?? TimeZoneHelper.GetRDNow().Date;

            var model = new ReportViewModel
            {
                StartDate = targetDate,
                EndDate = targetDate,
                ReportType = reportType ?? "Asistencia"
            };

            if (!string.IsNullOrEmpty(reportType))
            {
                model.Results = await GetReportData(model);
            }

            return View(model);
        }

        // POST: /Report/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ReportViewModel model)
        {
            model.Results = await GetReportData(model);
            return View(model);
        }

        // GET: /Report/ExportToExcel 
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string reportType, DateTime startDate, DateTime endDate)
        {
            var model = new ReportViewModel { ReportType = reportType, StartDate = startDate, EndDate = endDate };
            var data = await GetReportData(model);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte");

                // 2. Cabeceras Fijas
                worksheet.Cells[1, 1].Value = "Fecha";
                worksheet.Cells[1, 2].Value = "Empleado";
                worksheet.Cells[1, 3].Value = "Num. Empleado";
                worksheet.Cells[1, 4].Value = "Departamento";

                int colIndex = 5;

                // 3. Cabeceras Dinámicas
                if (reportType == "Tardanzas")
                {
                    worksheet.Cells[1, colIndex++].Value = "Hora Entrada";
                    worksheet.Cells[1, colIndex++].Value = "Hora Esperada";
                    worksheet.Cells[1, colIndex++].Value = "Diferencia";
                }
                else if (reportType == "Ausentes")
                {
                    worksheet.Cells[1, colIndex++].Value = "Estado";
                    worksheet.Cells[1, colIndex++].Value = "Observación";
                }
                else if (reportType == "HistorialSolicitudes")
                {
                    worksheet.Cells[1, colIndex++].Value = "Tipo";
                    worksheet.Cells[1, colIndex++].Value = "Desde";
                    worksheet.Cells[1, colIndex++].Value = "Hasta";
                    worksheet.Cells[1, colIndex++].Value = "Estado";
                    worksheet.Cells[1, colIndex++].Value = "Comentarios";
                }
                else // Asistencia Normal
                {
                    worksheet.Cells[1, colIndex++].Value = "Hora Entrada";
                    worksheet.Cells[1, colIndex++].Value = "Hora Salida";
                    worksheet.Cells[1, colIndex++].Value = "Horas Totales";
                }

                // Estilo de Cabecera
                worksheet.Cells[1, 1, 1, colIndex - 1].Style.Font.Bold = true;

                // 4. Llenar Datos
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = item.Date.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 2].Value = item.EmployeeName;
                    worksheet.Cells[row, 3].Value = item.EmployeeNumber;
                    worksheet.Cells[row, 4].Value = item.Department;

                    int dynamicCol = 5;

                    if (reportType == "Tardanzas")
                    {
                        worksheet.Cells[row, dynamicCol++].Value = item.CheckIn?.ToString("hh\\:mm tt");
                        worksheet.Cells[row, dynamicCol++].Value = TimeZoneHelper.GetRDNow().Date.Add(item.ExpectedTime ?? TimeSpan.Zero).ToString("hh\\:mm tt");
                        var delay = item.CheckIn?.TimeOfDay - item.ExpectedTime;
                        worksheet.Cells[row, dynamicCol++].Value = delay?.ToString(@"hh\:mm");
                    }
                    else if (reportType == "Ausentes")
                    {
                        worksheet.Cells[row, dynamicCol++].Value = item.Status;
                        worksheet.Cells[row, dynamicCol++].Value = item.Comments;
                    }
                    else if (reportType == "HistorialSolicitudes")
                    {
                        worksheet.Cells[row, dynamicCol++].Value = item.LeaveType;
                        worksheet.Cells[row, dynamicCol++].Value = item.LeaveStartDate.ToString("dd/MM/yyyy");
                        worksheet.Cells[row, dynamicCol++].Value = item.LeaveEndDate.ToString("dd/MM/yyyy");
                        worksheet.Cells[row, dynamicCol++].Value = item.Status;
                        worksheet.Cells[row, dynamicCol++].Value = item.Comments;
                    }
                    else
                    {
                        worksheet.Cells[row, dynamicCol++].Value = item.CheckIn?.ToString("hh\\:mm tt");
                        worksheet.Cells[row, dynamicCol++].Value = item.CheckOut.HasValue ? item.CheckOut.Value.ToString("hh\\:mm tt") : "N/A";
                        worksheet.Cells[row, dynamicCol++].Value = (item.CheckOut.HasValue && item.CheckIn.HasValue)
                            ? Math.Round((item.CheckOut.Value - item.CheckIn.Value).TotalHours, 2) : 0;
                    }
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                await package.SaveAsAsync(stream);
                stream.Position = 0;
                string fileName = $"Reporte_{reportType}_{startDate:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // GET: /Report/ExportToPdf
        [HttpGet]
        public async Task<IActionResult> ExportToPdf(string reportType, DateTime startDate, DateTime endDate)
        {
            var model = new ReportViewModel { ReportType = reportType, StartDate = startDate, EndDate = endDate };
            var data = await GetReportData(model);

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Encabezado
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text($"Reporte: {reportType}").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"Rango: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}").FontSize(12);
                        });
                    });

                    // Tabla Dinámica
                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        // Definición de Columnas
                        table.ColumnsDefinition(columns =>
                        {
                            // Columnas Fijas (Fecha, Empleado, Num. Empleado, Dept)
                            columns.ConstantColumn(70); // Fecha (dd/MM)
                            columns.RelativeColumn(1);  // Empleado
                            columns.ConstantColumn(60); // Num. Empleado
                            columns.RelativeColumn(1);  // Dept

                            // Columnas Dinámicas
                            if (reportType == "Tardanzas")
                            {
                                columns.ConstantColumn(60); // Entrada
                                columns.ConstantColumn(60); // Esperada
                                columns.ConstantColumn(50); // Dif 
                            }
                            else if (reportType == "Ausentes")
                            {
                                columns.ConstantColumn(60); // Estado
                                columns.RelativeColumn(1.5f); // Obs
                            }
                            else if (reportType == "HistorialSolicitudes")
                            {
                                columns.RelativeColumn(1);  // Tipo
                                columns.ConstantColumn(70); // Desde
                                columns.ConstantColumn(70); // Hasta
                                columns.ConstantColumn(60); // Estado
                                columns.RelativeColumn(1.5f); // Comentarios
                            }
                            else // Asistencia Normal
                            {
                                columns.ConstantColumn(60); // Entrada
                                columns.ConstantColumn(60); // Salida
                                columns.ConstantColumn(50); // Horas
                            }
                        });

                        // Cabecera
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Fecha");
                            header.Cell().Element(CellStyle).Text("Empleado");
                            header.Cell().Element(CellStyle).Text("Num. Emp.");
                            header.Cell().Element(CellStyle).Text("Dept");

                            if (reportType == "Tardanzas")
                            {
                                header.Cell().Element(CellStyle).Text("Entrada");
                                header.Cell().Element(CellStyle).Text("Esperada");
                                header.Cell().Element(CellStyle).Text("Dif");
                            }
                            else if (reportType == "Ausentes")
                            {
                                header.Cell().Element(CellStyle).Text("Estado");
                                header.Cell().Element(CellStyle).Text("Obs");
                            }
                            else if (reportType == "HistorialSolicitudes")
                            {
                                header.Cell().Element(CellStyle).Text("Tipo");
                                header.Cell().Element(CellStyle).Text("Desde");
                                header.Cell().Element(CellStyle).Text("Hasta");
                                header.Cell().Element(CellStyle).Text("Estado");
                                header.Cell().Element(CellStyle).Text("Comentarios");
                            }
                            else // Asistencia Normal
                            {
                                header.Cell().Element(CellStyle).Text("Entrada");
                                header.Cell().Element(CellStyle).Text("Salida");
                                header.Cell().Element(CellStyle).Text("Horas");
                            }

                            static IContainer CellStyle(IContainer container) =>
                                container.Background(Colors.Grey.Lighten3).Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        // Datos
                        foreach (var item in data)
                        {
                            table.Cell().Element(BlockStyle).Text(item.Date.ToString("dd/MM/yyyy"));
                            table.Cell().Element(BlockStyle).Text(item.EmployeeName);
                            table.Cell().Element(BlockStyle).Text(item.EmployeeNumber);
                            table.Cell().Element(BlockStyle).Text(item.Department);

                            if (reportType == "Tardanzas")
                            {
                                table.Cell().Element(BlockStyle).Text(item.CheckIn?.ToString("hh\\:mm tt"));
                                table.Cell().Element(BlockStyle).Text(TimeZoneHelper.GetRDNow().Date.Add(item.ExpectedTime ?? TimeSpan.Zero).ToString("hh\\:mm tt"));
                                var delay = item.CheckIn?.TimeOfDay - item.ExpectedTime;
                                table.Cell().Element(BlockStyle).Text(delay?.ToString(@"hh\:mm")).FontColor(Colors.Red.Medium);
                            }
                            else if (reportType == "Ausentes")
                            {
                                table.Cell().Element(BlockStyle).Text(item.Status).FontColor(Colors.Red.Medium);
                                table.Cell().Element(BlockStyle).Text(item.Comments);
                            }
                            else if (reportType == "HistorialSolicitudes")
                            {
                                table.Cell().Element(BlockStyle).Text(item.LeaveType);
                                table.Cell().Element(BlockStyle).Text(item.LeaveStartDate.ToString("dd/MM/yyyy"));
                                table.Cell().Element(BlockStyle).Text(item.LeaveEndDate.ToString("dd/MM/yyyy"));
                                table.Cell().Element(BlockStyle).Text(item.Status);
                                table.Cell().Element(BlockStyle).Text(item.Comments);
                            }
                            else // Asistencia Normal
                            {
                                table.Cell().Element(BlockStyle).Text(item.CheckIn?.ToString("hh\\:mm tt"));
                                table.Cell().Element(BlockStyle).Text(item.CheckOut.HasValue ? item.CheckOut.Value.ToString("hh\\:mm tt") : "--");
                                table.Cell().Element(BlockStyle).Text((item.CheckOut.HasValue && item.CheckIn.HasValue)
                                    ? Math.Round((item.CheckOut.Value - item.CheckIn.Value).TotalHours, 2).ToString() : "-");
                            }

                            static IContainer BlockStyle(IContainer container) =>
                                container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5);
                        }
                    });

                    page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); });
                });
            });

            return File(document.GeneratePdf(), "application/pdf", $"Reporte_{reportType}_{startDate:yyyyMMdd}.pdf");
        }

        private async Task<List<ReportItem>> GetReportData(ReportViewModel model)
        {
            var results = new List<ReportItem>();
            var filterEnd = model.EndDate.Date.AddDays(1).AddTicks(-1);
            var filterStart = model.StartDate.Date;

            // --- BLOQUE PARA ASISTENCIA NORMAL Y TARDANZAS ---
            if (model.ReportType == "Asistencia" || model.ReportType == "Tardanzas" || model.ReportType == "Presentes")
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

                results = rawResults.Select(x => new ReportItem
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
            // --- BLOQUE PARA AUSENTES ---
            else if (model.ReportType == "Ausentes")
            {
                var activeUsers = await _userManager.Users
                    .Include(u => u.Department)
                    .Where(u => u.LockoutEnd == null) 
                    .ToListAsync();

                var attendanceInRange = await _context.AttendanceRecords
                    .Where(r => r.CheckInTime >= filterStart && r.CheckInTime <= filterEnd)
                    .Select(r => new { r.ApplicationUserId, r.CheckInTime, r.CheckOutTime })
                    .ToListAsync();

                for (var day = filterStart; day <= model.EndDate.Date; day = day.AddDays(1))
                {
                    //if (day.DayOfWeek == DayOfWeek.Sunday) continue;

                    foreach (var user in activeUsers)
                    {
                        // Filtrar registros del usuario para el día actual
                        var userAttendanceForDay = attendanceInRange
                            .Where(r => r.ApplicationUserId == user.Id && r.CheckInTime.Date == day)
                            .ToList();

                        string comments = string.Empty;
                        bool isAbsent = false;

                        if (!userAttendanceForDay.Any())
                        {
                            //No hay CheckIn.
                            isAbsent = true;
                            comments = "Sin registro de entrada ni salida";
                        }

                        if (isAbsent)
                        {
                            results.Add(new ReportItem
                            {
                                EmployeeName = user.FirstName + " " + user.LastName,
                                EmployeeNumber = user.EmployeeNumber,
                                Department = user.Department?.Name ?? "N/A",
                                Date = day,
                                Status = "Ausente",
                                Comments = comments
                            });
                        }
                    }
                }
                results = results.OrderBy(r => r.Date).ThenBy(r => r.EmployeeName).ToList();
            }
            // --- BLOQUE PARA SOLICITUDES ---
            else if (model.ReportType == "HistorialSolicitudes")
            {
                var query = from lr in _context.LeaveRequests
                            join u in _context.Users on lr.ApplicationUserId equals u.Id
                            join d in _context.Departments on u.DepartmentId equals d.Id into departments
                            from dept in departments.DefaultIfEmpty()
                            where lr.RequestDate >= filterStart && lr.RequestDate <= filterEnd
                            select new
                            {
                                Request = lr,
                                User = u,
                                Department = dept
                            };

                var rawResults = await query.OrderByDescending(x => x.Request.RequestDate).ToListAsync();

                results = rawResults.Select(x => new ReportItem
                {
                    EmployeeName = x.User.FirstName + " " + x.User.LastName,
                    EmployeeNumber = x.User.EmployeeNumber,
                    Department = x.Department?.Name ?? "N/A",

                    Date = x.Request.RequestDate,
                    Status = x.Request.Status.ToString(),

                    LeaveStartDate = x.Request.StartDate,
                    LeaveEndDate = x.Request.EndDate,
                    LeaveType = x.Request.Type.ToString(),

                    Comments = x.Request.Status == LeaveStatus.Rejected
                               ? $"Rechazo: {x.Request.RejectionReason}"
                               : string.Empty
                }).ToList();
            }

            return results;
        }
    }
}