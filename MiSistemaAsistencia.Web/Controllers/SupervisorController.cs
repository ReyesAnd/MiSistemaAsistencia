using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Infrastructure;
using MiSistemaAsistencia.Infrastructure.Data;

namespace MiSistemaAsistencia.Web.Controllers
{
    //[Authorize(Roles = "Supervisor, Administrador")]
    [Authorize(Roles = "Administrador")]
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupervisorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Supervisor
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Supervisor/AssignSchedules
        public async Task<IActionResult> AssignSchedules()
        {
            var users = await _userManager.Users.ToListAsync();
            ViewBag.Schedules = await _context.WorkSchedules.ToListAsync();

            return View(users);
        }

        // POST: /Supervisor/AssignScheduleToUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignScheduleToUser(string userId, int scheduleId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var schedule = await _context.WorkSchedules.FindAsync(scheduleId);

            if (user == null || schedule == null)
            {
                TempData["ErrorMessage"] = "Usuario u horario no encontrado.";
                return RedirectToAction("AssignSchedules");
            }

            user.WorkScheduleId = scheduleId;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"Horario '{schedule.Name}' asignado a {user.FirstName}.";
            return RedirectToAction("AssignSchedules");
        }
    }
}