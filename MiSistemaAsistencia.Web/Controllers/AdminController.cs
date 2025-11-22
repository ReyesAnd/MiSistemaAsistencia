using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Domain;
using MiSistemaAsistencia.Infrastructure;
using MiSistemaAsistencia.Infrastructure.Data;
using MiSistemaAsistencia.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiSistemaAsistencia.Web.Models;

namespace MiSistemaAsistencia.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Admin
        public IActionResult Index()
        {
            return View();
        }

        // --- Gestión de Usuarios ---

        // GET: /Admin/UserManagement
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UserManagement(string searchString)
        {
            var users = await _context.Users
                            //.Include(u => u.SupervisorId)
                            .Include(u => u.Position)
                            .ToListAsync();

            var userViewModels = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userViewModels.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    EmployeeNumber = user.EmployeeNumber,
                    PositionName = user.Position?.Name,
                    LockoutEnd = user.LockoutEnd,
                    //SupervisorId = user.SupervisorId,
                    SystemRole = roles.FirstOrDefault() ?? "Sin Rol Asignado",
                });
            }

            return View(userViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = $"Usuario {user.UserName} desactivado exitosamente.";
            }
            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Quita la fecha de bloqueo (lo activa de inmediato)
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"Usuario {user.UserName} activado exitosamente.";
            }
            return RedirectToAction("UserManagement");
        }

        // --- [HttpGet] EditUser ---
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var supervisorRoleId = (await _roleManager.FindByNameAsync("Supervisor")).Id;
            var adminRoleId = (await _roleManager.FindByNameAsync("Administrador")).Id;

            var supervisorList = await _userManager.GetUsersInRoleAsync("Supervisor");
            var adminList = await _userManager.GetUsersInRoleAsync("Administrador");

            ViewData["SupervisorList"] = new SelectList(supervisorList, "Id", "Email", user.SupervisorId);
            ViewData["AdminList"] = new SelectList(adminList, "Id", "Email", user.SupervisorId);

            var userRoles = await _userManager.GetRolesAsync(user);
            ViewData["UserRole"] = userRoles.FirstOrDefault();

            var userRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Empleado";

            ViewData["PositionId"] = new SelectList(await _context.Positions.ToListAsync(), "Id", "Name", user.PositionId);
            ViewData["DepartmentId"] = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", user.DepartmentId);
            ViewData["WorkScheduleId"] = new SelectList(await _context.WorkSchedules.ToListAsync(), "Id", "Name", user.WorkScheduleId);
            ViewData["RoleName"] = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", userRole);

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                EmployeeNumber = user.EmployeeNumber,
                PositionId = user.PositionId,
                DepartmentId = user.DepartmentId,
                WorkScheduleId = user.WorkScheduleId,
                RoleName = userRole
            };

            return View(viewModel);
        }

        // --- [HttpPost] EditUser ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["PositionId"] = new SelectList(await _context.Positions.ToListAsync(), "Id", "Name", model.PositionId);
                ViewData["DepartmentId"] = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", model.DepartmentId);
                ViewData["WorkScheduleId"] = new SelectList(await _context.WorkSchedules.ToListAsync(), "Id", "Name", model.WorkScheduleId);
                ViewData["RoleName"] = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", model.RoleName);

                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PositionId = model.PositionId;
            user.DepartmentId = model.DepartmentId;
            user.WorkScheduleId = model.WorkScheduleId;

            var updateResult = await _userManager.UpdateAsync(user);
            
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.RoleName))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.RoleName);
            }

            TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
            return RedirectToAction("UserManagement");
        }

        // GET: /Admin/CreateUser
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.Roles = _roleManager.Roles.ToList();

            var supervisorList = await _userManager.GetUsersInRoleAsync("Supervisor");
            var adminList = await _userManager.GetUsersInRoleAsync("Administrador");

            ViewData["SupervisorList"] = new SelectList(supervisorList, "Id", "Email");
            ViewData["AdminList"] = new SelectList(adminList, "Id", "Email");

            return View();
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmployeeNumber = model.EmployeeNumber,
                    HireDate = model.HireDate,
                    AvailableVacationDays = 15,
                    SupervisorId = model.SupervisorId
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.RoleName);
                    TempData["SuccessMessage"] = "Usuario creado exitosamente.";
                    return RedirectToAction("UserManagement");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            ViewBag.Roles = _roleManager.Roles.ToList();

            var supervisorList = await _userManager.GetUsersInRoleAsync("Supervisor");
            var adminList = await _userManager.GetUsersInRoleAsync("Administrador");
            ViewData["SupervisorList"] = new SelectList(supervisorList, "Id", "Email");
            ViewData["AdminList"] = new SelectList(adminList, "Id", "Email");

            return View(model);
        }

        // --- Gestión de Horarios ---

        // GET: /Admin/Schedules
        public async Task<IActionResult> Schedules()
        {
            var schedules = await _context.WorkSchedules.ToListAsync();
            return View(schedules);
        }

        // GET: /Admin/CreateSchedule
        public IActionResult CreateSchedule()
        {
            return View();
        }

        // POST: /Admin/CreateSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule(WorkSchedule schedule)
        {
            if (ModelState.IsValid)
            {
                _context.WorkSchedules.Add(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Horario creado exitosamente.";
                return RedirectToAction("Schedules");
            }
            return View(schedule);
        }

        // --- GET: /Admin/EditSchedule/{id} ---
        [HttpGet]
        public async Task<IActionResult> EditSchedule(int id)
        {
            var schedule = await _context.WorkSchedules.FindAsync(id);

            if (schedule == null)
            {
                return NotFound();
            }

            var viewModel = new EditScheduleViewModel
            {
                Id = schedule.Id,
                Name = schedule.Name,
                ExpectedCheckIn = schedule.ExpectedCheckIn,
                ExpectedCheckOut = schedule.ExpectedCheckOut
            };

            return View(viewModel);
        }

        // --- POST: /Admin/EditSchedule ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchedule(EditScheduleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var schedule = await _context.WorkSchedules.FindAsync(model.Id);

            if (schedule == null)
            {
                return NotFound();
            }

            // Mapear y actualizar
            schedule.Name = model.Name;
            schedule.ExpectedCheckIn = model.ExpectedCheckIn;
            schedule.ExpectedCheckOut = model.ExpectedCheckOut;

            _context.Update(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Horario '{schedule.Name}' actualizado exitosamente.";
            return RedirectToAction("Schedules");
        }

        // --- POST: /Admin/DeleteSchedule/{id} ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.WorkSchedules.FindAsync(id);
            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Horario no encontrado.";
                return RedirectToAction("Schedules");
            }

            //Verificar si hay empleados asignados a este horario
            var isScheduleInUse = await _userManager.Users.AnyAsync(u => u.WorkScheduleId == id);

            if (isScheduleInUse)
            {
                TempData["ErrorMessage"] = $"No se puede eliminar el horario '{schedule.Name}' porque está asignado a uno o más empleados.";

                return RedirectToAction("Schedules");
            }

            _context.WorkSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Horario '{schedule.Name}' eliminado exitosamente.";
            return RedirectToAction("Schedules");
        }
    }
}