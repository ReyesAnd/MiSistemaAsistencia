using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Application; 
using MiSistemaAsistencia.Infrastructure;
using MiSistemaAsistencia.Infrastructure.Data;
using MiSistemaAsistencia.Web.Models;
using MiSistemaAsistencia.Domain;

namespace MiSistemaAsistencia.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context, IAttendanceService attendanceService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _attendanceService = attendanceService;
            _userManager = userManager;
        }

        //// GET: /Dashboard/Index
        //[HttpGet]
        //public async Task<IActionResult> Index()
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    ViewBag.UserName = user.FirstName;
        //    return View();
        //}

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            EmployeeDashboardViewModel viewModel = null;

            if (User.IsInRole("Empleado") || User.IsInRole("Supervisor"))
            {
                viewModel = new EmployeeDashboardViewModel();

                var user = await _context.Users.FindAsync(userId);
                viewModel.AvailableVacationDays = user?.AvailableVacationDays ?? 0;

                var lastRecord = await _context.AttendanceRecords
                    .Where(r => r.ApplicationUserId == userId)
                    .OrderByDescending(r => r.CheckInTime)
                    .FirstOrDefaultAsync();

                if (lastRecord != null)
                {
                    if (lastRecord.CheckOutTime == null)
                    {
                        viewModel.CurrentStatus = "Activo";
                    }
                    else
                    {
                        viewModel.CurrentStatus = "Inactivo";
                    }
                    viewModel.LastCheckInTime = lastRecord.CheckInTime;
                }
                else
                {
                    viewModel.CurrentStatus = "Sin Registro";
                }

                DateTime today = DateTime.Today;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateTime startOfWeek = today.AddDays(-1 * diff).Date;

                var recordsThisWeek = await _context.AttendanceRecords
                    .Where(r => r.ApplicationUserId == userId &&
                                r.CheckInTime >= startOfWeek &&
                                r.CheckOutTime != null)
                    .ToListAsync();

                double totalHours = 0;
                foreach (var record in recordsThisWeek)
                {
                    totalHours += (record.CheckOutTime.Value - record.CheckInTime).TotalHours;
                }
                viewModel.HoursWorkedThisWeek = Math.Round(totalHours, 2);

                viewModel.PendingLeaveRequests = await _context.LeaveRequests
                    .CountAsync(lr => lr.ApplicationUserId == userId && lr.Status == LeaveStatus.Pending);
            }

            if (User.IsInRole("Administrador"))
            {
                var adminViewModel = new AdminDashboardViewModel();
                var today = DateTime.Today;

                var lateArrivalsQuery = from attendance in _context.AttendanceRecords
                                        join user in _context.Users on attendance.ApplicationUserId equals user.Id
                                        join schedule in _context.WorkSchedules on user.WorkScheduleId equals schedule.Id
                                        where attendance.CheckInTime.Date == today
                                        select new
                                        {
                                            ActualTime = attendance.CheckInTime.TimeOfDay,
                                            ExpectedTime = schedule.ExpectedCheckIn
                                        };

                var lateCount = await lateArrivalsQuery
                    .CountAsync(r => r.ActualTime > r.ExpectedTime);

                adminViewModel.LateArrivalsToday = lateCount;

                // Totales
                adminViewModel.TotalEmployees = await _userManager.Users.CountAsync();

                // Solicitudes Pendientes
                adminViewModel.PendingApprovalRequests = await _context.LeaveRequests
                    .CountAsync(lr => lr.Status == LeaveStatus.Pending);

                // Empleados Presentes
                adminViewModel.EmployeesPresent = await _context.AttendanceRecords
                    .CountAsync(r => r.CheckOutTime == null);

                // Empleados Ausentes
                adminViewModel.EmployeesAbsentToday = adminViewModel.TotalEmployees - adminViewModel.EmployeesPresent;

                // Cargar los últimos 10 registros.
                var recentActivityQuery = from attendance in _context.AttendanceRecords
                                          join user in _context.Users on attendance.ApplicationUserId equals user.Id
                                          where attendance.CheckInTime.Date == today || (attendance.CheckOutTime.HasValue && attendance.CheckOutTime.Value.Date == today)
                                          orderby attendance.CheckInTime descending
                                          select new
                                          {
                                              EmployeeName = user.FirstName + " " + user.LastName,
                                              attendance.CheckInTime,
                                              attendance.CheckOutTime
                                          };

                var recentRecords = await recentActivityQuery.Take(10).ToListAsync();

                foreach (var record in recentRecords)
                {
                    if (record.CheckOutTime.HasValue && record.CheckOutTime.Value > record.CheckInTime)
                    {
                        adminViewModel.RecentActivity.Add(new RecentActivityViewModel
                        {
                            EmployeeName = record.EmployeeName,
                            Type = "Salida",
                            Time = record.CheckOutTime.Value
                        });
                    }

                    adminViewModel.RecentActivity.Add(new RecentActivityViewModel
                    {
                        EmployeeName = record.EmployeeName,
                        Type = "Entrada",
                        Time = record.CheckInTime
                    });
                }

                ViewData["AdminModel"] = adminViewModel;
                return View();
            }

            return View(viewModel);
        }

            // POST: /Dashboard/ClockIn
            [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Empleado, Supervisor")]
        public async Task<IActionResult> ClockIn()
        {
            var userId = _userManager.GetUserId(User);
            var success = await _attendanceService.ClockIn(userId);

            if (success)
            {
                TempData["SuccessMessage"] = "¡Entrada registrada exitosamente!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error: Ya registraste tu entrada hoy.";
            }

            return RedirectToAction("Index");
        }

        // POST: /Dashboard/ClockOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Empleado, Supervisor")]
        public async Task<IActionResult> ClockOut()
        {
            var userId = _userManager.GetUserId(User);
            var success = await _attendanceService.ClockOut(userId);

            if (success)
            {
                TempData["SuccessMessage"] = "¡Salida registrada exitosamente!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error: Debes registrar tu entrada primero o ya registraste tu salida.";
            }

            return RedirectToAction("Index");
        }
    }
}