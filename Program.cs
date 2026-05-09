using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Middleware;
using NotebookValidator.Web.Services;
using QuestPDF.Infrastructure;
using Syncfusion.Licensing;
using System;
using NotebookValidator.Web.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// --- NUEVO: Registrar soporte para lectura de Excel (requerido por ExcelDataReader) ---
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF1cWWhBYVF3WmFZfVtgd19FY1ZQQWY/P1ZhSXxWdk1iXX5bc3dUQ2laU019XEI=");

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// --- Configuración de Servicios ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 1. Configurar Kestrel (Servidor interno por defecto de ASP.NET)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 2147483648; // 2 GB
});

// 2. Configurar IIS (Si ejecutas la app usando IIS Express en Visual Studio)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 2147483648; // 2 GB
});

// 3. Configurar los límites de lectura de formularios en toda la aplicación
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 2147483648; // 2 GB
    options.MemoryBufferThreshold = int.MaxValue;
});

builder.Services.AddHostedService<EmailBotBackgroundService>();
builder.Services.AddScoped<DataProfilingService>();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<NotebookValidatorService>(sp =>
    new NotebookValidatorService(
        sp.GetRequiredService<ApplicationDbContext>())
);
builder.Services.AddHttpClient();
builder.Services.AddScoped<TempTableService>();
builder.Services.AddScoped<ParameterValidationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<TestAIService>();
builder.Services.AddScoped<DocumentationService>();
builder.Services.AddScoped<WordExportService>();
builder.Services.AddScoped<JobTransformationService>();

// --- NUEVO: Inyectar el servicio de Cuadratura ---
builder.Services.AddScoped<ICuadraturaService, CuadraturaService>();

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
app.UseMiddleware<ForcePasswordChangeMiddleware>();
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
