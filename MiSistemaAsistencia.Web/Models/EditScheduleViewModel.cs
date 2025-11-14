using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MiSistemaAsistencia.Web.Models
{
    public class EditScheduleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del horario es obligatorio.")]
        [Display(Name = "Nombre del Horario")]
        public string Name { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
        [Display(Name = "Hora de Inicio")]
        [DataType(DataType.Time)]
        public TimeSpan ExpectedCheckIn { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria.")]
        [Display(Name = "Hora de Fin")]
        [DataType(DataType.Time)]
        public TimeSpan ExpectedCheckOut { get; set; }
    }
}