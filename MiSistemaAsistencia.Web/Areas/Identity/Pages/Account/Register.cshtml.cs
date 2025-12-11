using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Application;
using MiSistemaAsistencia.Domain;
using MiSistemaAsistencia.Infrastructure;
using MiSistemaAsistencia.Infrastructure.Data;
using MiSistemaAsistencia.Infrastructure.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace MiSistemaAsistencia.Web.Areas.Identity.Pages.Account
{
    [Authorize(Roles = "Administrador")]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmployeeNumberService _numberService;


        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager, 
            ApplicationDbContext context, 
            ILogger<RegisterModel> logger,
            IEmployeeNumberService numberService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager; 
            _context = context; 
            _logger = logger;
            _numberService = numberService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        // --- PROPIEDADES PARA LOS DROPDOWNS ---
        public SelectList RoleOptions { get; set; }
        public SelectList DepartmentOptions { get; set; }
        public SelectList ScheduleOptions { get; set; }
        public SelectList PositionOptions { get; set; }

        // Entrada con TODOS los campos necesarios
        public class InputModel
        {
            [Required(ErrorMessage = "El correo es obligatorio")]
            [EmailAddress]
            [Display(Name = "Correo Electrónico")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria")]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y un máximo de {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
            public string ConfirmPassword { get; set; }

            // --- CAMPOS PERSONALIZADOS ---

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [Display(Name = "Nombre")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "El apellido es obligatorio")]
            [Display(Name = "Apellido")]
            public string LastName { get; set; }

            //[Required(ErrorMessage = "El número de empleado es obligatorio")]
            //[Display(Name = "Número de Empleado")]
            //public string EmployeeNumber { get; set; }

            [Required(ErrorMessage = "Debe seleccionar un rol")]
            [Display(Name = "Rol del Sistema")]
            public string RoleName { get; set; }

            [Required(ErrorMessage = "Debe seleccionar un departamento")]
            [Display(Name = "Departamento")]
            public int DepartmentId { get; set; }

            [Required(ErrorMessage = "Debe seleccionar un horario")]
            [Display(Name = "Horario de Trabajo")]
            public int WorkScheduleId { get; set; }

            [Required(ErrorMessage = "Debe seleccionar un puesto")]
            [Display(Name = "Puesto")]
            public int PositionId { get; set; }

            [Required(ErrorMessage = "La Fecha de Ingreso es obligatoria.")]
            [Display(Name = "Fecha de Contratación")]
            [DataType(DataType.Date)]
            public DateTime HireDate { get; set; } //= TimeZoneHelper.GetRDNow();   Se va a habilitar en el formulario que se ingrese manualmente.

            [Display(Name = "Supervisor/Administrador")]
            public string? SupervisorId { get; set; }
        }


        // Carga las listas para los dropdowns
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            RoleOptions = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name");
            DepartmentOptions = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name");
            ScheduleOptions = new SelectList(await _context.WorkSchedules.ToListAsync(), "Id", "Name");
            PositionOptions = new SelectList(await _context.Positions.ToListAsync(), "Id", "Name");

            var supervisorUsers = await _userManager.GetUsersInRoleAsync("Supervisor");
            var adminUsers = await _userManager.GetUsersInRoleAsync("Administrador");

            ViewData["SupervisorList"] = new SelectList(supervisorUsers, "Id", "FullName");
            ViewData["AdminList"] = new SelectList(adminUsers, "Id", "FullName");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var newEmployeeNumber = await _numberService.GetNextEmployeeNumberAsync();

                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    EmployeeNumber = newEmployeeNumber,
                    DepartmentId = Input.DepartmentId,
                    WorkScheduleId = Input.WorkScheduleId,
                    PositionId = Input.PositionId,
                    HireDate = Input.HireDate, //TimeZoneHelper.GetRDNow(),  Se cambio para ponerlo manual.
                    AvailableVacationDays = 0, 
                    EmailConfirmed = true,
                    SupervisorId = Input.SupervisorId
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("El administrador creó una nueva cuenta.");

                    await _userManager.AddToRoleAsync(user, Input.RoleName);

                    TempData["SuccessMessage"] = $"¡Empleado '{user.FirstName} {user.LastName}' creado exitosamente!";
                    return RedirectToAction("UserManagement", "Admin");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            await OnGetAsync(returnUrl);

            return Page();
        }
    }
}