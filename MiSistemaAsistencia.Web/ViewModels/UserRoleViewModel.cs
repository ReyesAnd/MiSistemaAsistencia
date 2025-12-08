namespace MiSistemaAsistencia.Web.ViewModels
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PositionName { get; set; }
        public string SupervisorId { get; set; }
        public string SystemRole { get; set; }
        public String HireDate { get; set; }
        public string EmployeeNumber { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}
