using MiSistemaAsistencia.Infrastructure.Helpers;
using System.ComponentModel.DataAnnotations;

namespace MiSistemaAsistencia.Web.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [Display(Name = "Apellido")]
        public string LastName { get; set; }

        [Display(Name = "Número de Empleado")]
        public string EmployeeNumber { get; set; }

        [Required(ErrorMessage = "La Fecha de Ingreso es obligatoria.")]
        [Display(Name = "Fecha de Contratación")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } //= TimeZoneHelper.GetRDNow();   Se va a habilitar en el formulario que se ingrese manualmente.

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        [Display(Name = "Rol del Sistema")]
        public string RoleName { get; set; }
        
        [Display(Name = "Supervisor")]
        public string SupervisorId { get; set; }
    }
}