using System.ComponentModel.DataAnnotations;

namespace MiSistemaAsistencia.Web.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Display(Name = "Núm. Empleado")]
        public string EmployeeNumber { get; set; }

        [Required(ErrorMessage = "El Nombre es obligatorio.")]
        [Display(Name = "Nombre")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "El Apellido es obligatorio.")]
        [Display(Name = "Apellido")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "El Email es obligatorio.")]
        [EmailAddress]
        public string Email { get; set; }

        // --- Dropdown Values (Claves Foráneas) ---
        
        [Required(ErrorMessage = "Debe seleccionar un puesto.")]
        [Display(Name = "Puesto")]
        public int PositionId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un departamento.")]
        [Display(Name = "Departamento")]
        public int DepartmentId { get; set; }

        [Display(Name = "Horario")]
        public int? WorkScheduleId { get; set; }

        // --- Rol de Sistema ---
        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        [Display(Name = "Rol")]
        public string RoleName { get; set; }
    }
}