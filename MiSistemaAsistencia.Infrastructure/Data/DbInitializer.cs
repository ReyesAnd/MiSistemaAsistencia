using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Application;
using MiSistemaAsistencia.Domain;
using MiSistemaAsistencia.Infrastructure.Helpers;
using System.Linq;

namespace MiSistemaAsistencia.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IEmployeeNumberService numberService)
        {
            if (!await context.Positions.AnyAsync())
            {
                var positions = new Position[]
                {
                    new Position { Name = "Gerente General" },
                    new Position { Name = "Analista de Sistemas" },
                    new Position { Name = "Soporte Técnico" }
                };
                await context.Positions.AddRangeAsync(positions);
                await context.SaveChangesAsync();
            }

            // ===================== DEPARTMENTS =====================
            if (!await context.Departments.AnyAsync())
            {
                var departments = new[]
                {
                new Department { Name = "Administración" },
                new Department { Name = "Recursos Humanos" },
                new Department { Name = "Tecnología" }
            };
                await context.Departments.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }

            // ===================== WORK SCHEDULES =====================
            if (!await context.WorkSchedules.AnyAsync())
            {
                var schedules = new[]
                {
                new WorkSchedule { Name = "Horario Regular", ExpectedCheckIn = new TimeSpan(8,0,0), ExpectedCheckOut = new TimeSpan(17,0,0) },
                new WorkSchedule { Name = "Medio Tiempo", ExpectedCheckIn = new TimeSpan(8,0,0), ExpectedCheckOut = new TimeSpan(12,0,0) }
            };
                await context.WorkSchedules.AddRangeAsync(schedules);
                await context.SaveChangesAsync();
            }

            // CREAR ROLES
            string[] roleNames = { "Administrador", "Supervisor", "Empleado" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // CREAR USUARIO ADMIN
            var adminEmail = "admin@misistema.com";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var newEmployeeNumber = await numberService.GetNextEmployeeNumberAsync();
                var adminPosition = await context.Positions.FirstOrDefaultAsync();
                var department = await context.Departments.FirstAsync();
                var schedule = await context.WorkSchedules.FirstAsync();

                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    EmployeeNumber = newEmployeeNumber,
                    HireDate = TimeZoneHelper.GetRDNow(),
                    PositionId = adminPosition.Id,
                    DepartmentId = department.Id,
                    AvailableVacationDays = 0,
                    WorkScheduleId = schedule.Id,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = false,
                    AccessFailedCount = 0
                };

                var result = await userManager.CreateAsync(adminUser, "Tempor@l98");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
                }
                else
                {
                    //var errors = result.Errors;
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creando usuario admin: {error.Code} - {error.Description}");
                    }
                }
            }
        }
    }
}