using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiSistemaAsistencia.Application;
using MiSistemaAsistencia.Domain;
using MiSistemaAsistencia.Infrastructure;
using MiSistemaAsistencia.Infrastructure.Data;
using MiSistemaAsistencia.Infrastructure.Services;
using OfficeOpenXml;
using System.ComponentModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.License.SetNonCommercialOrganization("Syncronix");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Configuracin del DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ROLES Y IDENTITY
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;       // requiere un número
    options.Password.RequireLowercase = true;   // requiere minúscula
    options.Password.RequireNonAlphanumeric = false; // No requiere símbolo
    options.Password.RequireUppercase = true;   // requiere mayúscula
    options.Password.RequiredLength = 8;         
    options.Password.RequiredUniqueChars = 1;

    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();
// ----------------------------------------------------------------

builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEmployeeNumberService, EmployeeNumberService>();

builder.Services.AddControllersWithViews();



var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// SEED DATA
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var numberService = services.GetRequiredService<IEmployeeNumberService>();

        await DbInitializer.Initialize(
            context,
            userManager,
            roleManager,
            numberService
        );

        //await DbInitializer.Initialize(userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
// ----------------------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();