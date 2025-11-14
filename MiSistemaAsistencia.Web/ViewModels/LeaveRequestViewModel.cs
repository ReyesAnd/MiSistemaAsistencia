using MiSistemaAsistencia.Domain;

namespace MiSistemaAsistencia.Web.ViewModels
{
    public class LeaveRequestViewModel
    {
        public int RequestId { get; set; }
        public string ApplicantName { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public LeaveType Type { get; set; }
        public LeaveStatus Status { get; set; }
    }
}