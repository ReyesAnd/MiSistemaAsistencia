using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Domain;
using MiSistemaAsistencia.Infrastructure;
using MiSistemaAsistencia.Infrastructure.Data;
using MiSistemaAsistencia.Web.ViewModels;

namespace MiSistemaAsistencia.Web.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaveRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /LeaveRequest 
        [Authorize(Roles = "Empleado,Supervisor")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var myRequests = await _context.LeaveRequests
                .Where(r => r.ApplicationUserId == userId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(myRequests);
        }

        // GET: /LeaveRequest/Create
        [Authorize(Roles = "Empleado,Supervisor")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /LeaveRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Empleado,Supervisor")]
        public async Task<IActionResult> Create(LeaveRequest request)
        {
            //request.ApplicationUserId = user.Id;
            //request.RequestUser = user;

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                if (request.EndDate < request.StartDate)
                {
                    ModelState.AddModelError("EndDate", "La fecha de fin no puede ser anterior a la de inicio.");
                    return View(request);
                }

                if (request.StartDate.Date < DateTime.Today)
                {
                    ModelState.AddModelError("StartDate", "No puedes realizar solicitudes para fechas pasadas.");
                    return View(request);
                }

                int requestedDays = (request.EndDate - request.StartDate).Days + 1;

                if (request.Type == LeaveType.Vacaciones)
                {
                    if (requestedDays > user.AvailableVacationDays)
                    {
                        ModelState.AddModelError(string.Empty, $"No tienes suficientes días. Solicitaste {requestedDays}, pero solo tienes {user.AvailableVacationDays} disponibles.");
                        return View(request);
                    }
                }

                request.ApplicationUserId = user.Id;
                request.RequestDate = DateTime.Now;
                request.Status = LeaveStatus.Pending;

                _context.Add(request);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Solicitud enviada exitosamente.";
                return RedirectToAction("Index");
            }

            return View(request);
        }

        // --- ACCIONES DE SUPERVISOR / ADMIN ---

        // GET: /LeaveRequest/Pending
        [Authorize(Roles = "Supervisor, Administrador")]
        public async Task<IActionResult> Pending()
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdministrator = User.IsInRole("Administrador");

            var pendingRequestsQuery = _context.LeaveRequests
                .Include(r => r.RequestUser)
                .Where(r => r.Status == LeaveStatus.Pending)
                .AsQueryable();

            if (isAdministrator)
            {
            }
            else if (User.IsInRole("Supervisor"))
            {
                pendingRequestsQuery = pendingRequestsQuery
                    .Where(r => r.RequestUser.SupervisorId == currentUserId);
            }

            var pendingRequests = await pendingRequestsQuery
                .Select(request => new LeaveRequestViewModel
                {
                    RequestId = request.Id,
                    ApplicantName = EF.Property<string>(request.RequestUser, "FirstName") + " " +
                                    EF.Property<string>(request.RequestUser, "LastName"),

                    RequestDate = request.RequestDate,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Type = request.Type,
                    Status = request.Status
                })
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(pendingRequests);
        }

        // POST: /LeaveRequest/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor, Administrador")]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.LeaveRequests.FindAsync(id);
            if (request == null) return NotFound();

            var user = await _context.Users.FindAsync(request.ApplicationUserId);
            if (user == null) return NotFound();

            request.Status = LeaveStatus.Approved;
            request.ApprovedByUserId = _userManager.GetUserId(User);

            if (request.Type == LeaveType.Vacaciones)
            {
                int requestedDays = (request.EndDate - request.StartDate).Days + 1;
                user.AvailableVacationDays -= requestedDays;
                _context.Update(user);
            }

            _context.Update(request);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Solicitud aprobada.";

            return RedirectToAction("Pending");
        }

        // POST: /LeaveRequest/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor, Administrador")]
        public async Task<IActionResult> Reject(int id, [FromForm] string rejectionReason)
        {
            var request = await _context.LeaveRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = LeaveStatus.Rejected;
            request.ApprovedByUserId = _userManager.GetUserId(User);
            request.RejectionReason = rejectionReason;

            _context.Update(request);
            await _context.SaveChangesAsync();
            TempData["WarningMessage"] = "Solicitud rechazada.";

            return RedirectToAction("Pending");
        }
    }
}