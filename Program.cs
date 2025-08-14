using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Services;
using System;
using QuestPDF.Infrastructure;
using Syncfusion.Licensing;

var builder = WebApplication.CreateBuilder(args);
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF1cWWhBYVF3WmFZfVtgd19FY1ZQQWY/P1ZhSXxWdk1iXX5bc3dUQ2laU019XEI=");

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// --- Configuración de Servicios ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<NotebookValidatorService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<TempTableService>();
builder.Services.AddScoped<ParameterValidationService>();
builder.Services.AddScoped<TestAIService>();
builder.Services.AddScoped<DocumentationService>();
builder.Services.AddScoped<WordExportService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();

// --- Configuración del Pipeline de Peticiones HTTP ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// --- INICIO DEL NUEVO BLOQUE DE CÓDIGO ---

// Este bloque se encarga de aplicar las migraciones y crear los datos iniciales.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 1. Aplica las migraciones de la base de datos automáticamente.
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        // 2. Ejecuta el Seeder para crear roles y el usuario admin (como ya lo teníamos).
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al aplicar las migraciones o al ejecutar el seeder.");
    }
}

// --- FIN DEL NUEVO BLOQUE DE CÓDIGO ---


app.Run();