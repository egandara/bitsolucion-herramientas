using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Middleware;
using NotebookValidator.Web.Services;
using QuestPDF.Infrastructure;
using Syncfusion.Licensing;
using System;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// --- Registrar soporte para lectura de Excel ---
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

// --- Configurar licencias desde appsettings/User Secrets ---
var syncfusionLicense = builder.Configuration["Licenses:Syncfusion"];
if (!string.IsNullOrEmpty(syncfusionLicense))
{
    SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
}

var questPdfLicense = builder.Configuration["Licenses:QuestPDF"];
if (questPdfLicense == "Community")
{
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
}
else if (!string.IsNullOrEmpty(questPdfLicense))
{
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Professional;
}

// --- Configuración de Servicios ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 1. Configurar Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 2147483648; // 2 GB
});

// 2. Configurar IIS
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 2147483648; // 2 GB
});

// 3. Configurar límites de formularios
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 2147483648; // 2 GB
    options.MemoryBufferThreshold = int.MaxValue;
});

builder.Services.AddScoped<NotebookBuilderService>();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
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
builder.Services.AddScoped<ICuadraturaService, CuadraturaService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- Pipeline de Peticiones HTTP ---
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

app.MapHub<NotebookValidator.Web.Hubs.ProfilingHub>("/profilingHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// --- Migraciones y Seeding ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al aplicar migraciones o ejecutar seeder.");
    }
}

app.Run();
