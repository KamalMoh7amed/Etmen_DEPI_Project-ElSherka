using Etmen_BLL.Mapping;
using Etmen_BLL.Repositories.IServices;
using Etmen_BLL.Repositories.Services;
using Etmen_DAL.DbContext;
using Etmen_DAL.Repositories.Implementations;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_DAL.Seed;
using Etmen_Domain.Entities;
using Mapster;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════
// 1. DATABASE CONTEXT
// ═══════════════════════════════════════════════════════════════
builder.Services.AddDbContext<EtmenDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.MigrationsAssembly(typeof(EtmenDbContext).Assembly.FullName);
            sql.CommandTimeout(60);
            sql.EnableRetryOnFailure(3);
        }
    )
);

// ═══════════════════════════════════════════════════════════════
// 2. IDENTITY
// ═══════════════════════════════════════════════════════════════
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password rules
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User rules
    options.User.RequireUniqueEmail = true;

    // Email confirmation
    options.SignIn.RequireConfirmedEmail = true;

    // Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<int>("Security:LockoutDurationInMinutes", 15));
    options.Lockout.MaxFailedAccessAttempts = builder.Configuration.GetValue<int>("Security:MaxLoginAttempts", 5);
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<EtmenDbContext>()
.AddDefaultTokenProviders();

// ── Cookie authentication ──────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Account/Login";
    options.LogoutPath = "/Auth/Account/Logout";
    options.AccessDeniedPath = "/Auth/Account/AccessDenied";

    options.Cookie.Name = "EtmenAuthCookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.ExpireTimeSpan = TimeSpan.FromDays(
        builder.Configuration.GetValue<int>("Security:CookieExpirationDays", 7));
    options.SlidingExpiration = true;
});

// ═══════════════════════════════════════════════════════════════
// 3. DATA ACCESS (Unit of Work)
// ═══════════════════════════════════════════════════════════════
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ═══════════════════════════════════════════════════════════════
// 4. BUSINESS LOGIC LAYER — ALL SERVICES
// ═══════════════════════════════════════════════════════════════

// Auth
builder.Services.AddScoped<IAuthService, AuthService>();

// Patient
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();

// Doctor
builder.Services.AddScoped<IDoctorService, DoctorService>();

// Appointments
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<INearbyService, NearbyService>();

// Health Monitoring
builder.Services.AddScoped<IRiskService, RiskService>();
builder.Services.AddScoped<ILabService, LabService>();
builder.Services.AddScoped<IAlertService, AlertService>();

// Communications
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAIChatService, AIChatService>();

// Family
builder.Services.AddScoped<IFamilyService, FamilyService>();

// Emergency
builder.Services.AddScoped<IEmergencyService, EmergencyService>();

// Crisis
builder.Services.AddScoped<ICrisisService, CrisisService>();
builder.Services.AddScoped<ICrisisRiskEngineService, CrisisRiskEngineService>();

// Admin
builder.Services.AddScoped<IAdminService, AdminService>();

// ═══════════════════════════════════════════════════════════════
// 5. MAPSTER — تسجيل الـ Mapping Profiles من BLL
// ═══════════════════════════════════════════════════════════════
var mapsterConfig = TypeAdapterConfig.GlobalSettings;
mapsterConfig.Scan(typeof(BLLMappingProfile).Assembly);   // BLL assembly
mapsterConfig.Compile();                                    // Early error detection

builder.Services.AddSingleton(mapsterConfig);

// ═══════════════════════════════════════════════════════════════
// 6. MVC
// ═══════════════════════════════════════════════════════════════
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });

builder.Services.AddRazorPages();

// ═══════════════════════════════════════════════════════════════
// 7. SESSION
// ═══════════════════════════════════════════════════════════════
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<int>("Security:SessionTimeoutMinutes", 60));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Name = "EtmenSession";
});

// ═══════════════════════════════════════════════════════════════
// 8. CACHING
// ═══════════════════════════════════════════════════════════════
builder.Services.AddMemoryCache();

// ═══════════════════════════════════════════════════════════════
// 9. HTTP CLIENT (for external APIs: OCR, AI, etc.)
// ═══════════════════════════════════════════════════════════════
builder.Services.AddHttpClient();

// ═══════════════════════════════════════════════════════════════
// 10. CORS
// ═══════════════════════════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
        policy
            .WithOrigins("https://localhost:5001", "https://localhost:7001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// ═══════════════════════════════════════════════════════════════
// 11. ANTI-FORGERY
// ═══════════════════════════════════════════════════════════════
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "EtmenAntiForgery";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ═══════════════════════════════════════════════════════════════
// 12. FILE UPLOAD LIMIT (10 MB)
// ═══════════════════════════════════════════════════════════════
builder.Services.Configure<FormOptions>(options =>
{
    int maxMb = builder.Configuration.GetValue<int>("FileUpload:MaxFileSizeInMB", 10);
    options.MultipartBodyLengthLimit = maxMb * 1024 * 1024;
});

// ═══════════════════════════════════════════════════════════════
// BUILD APP
// ═══════════════════════════════════════════════════════════════
var app = builder.Build();

// ═══════════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE
// ═══════════════════════════════════════════════════════════════

// ── Error Handling ─────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

// ── HTTPS ──────────────────────────────────────────────────────
app.UseHttpsRedirection();

// ── Static Files ───────────────────────────────────────────────
app.UseStaticFiles();

// ── Routing ────────────────────────────────────────────────────
app.UseRouting();

// ── CORS ───────────────────────────────────────────────────────
app.UseCors("AllowSpecificOrigin");

// ── Session ────────────────────────────────────────────────────
app.UseSession();

// ── Auth ───────────────────────────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();

// ── Controller Routes ──────────────────────────────────────────
// Areas route (Admin, Doctor, Patient areas)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ═══════════════════════════════════════════════════════════════
// DATABASE: MIGRATION + SEEDING
// ═══════════════════════════════════════════════════════════════
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    // Migrations: create a short-lived scope just for the DbContext
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<EtmenDbContext>();

        if (context.Database.GetPendingMigrations().Any())
        {
            startupLogger.LogInformation("Applying pending migrations...");
            await context.Database.MigrateAsync();
            startupLogger.LogInformation("Migrations applied successfully.");
        }
    }

    // Seeding: DataSeeder creates its own scope internally — pass root provider
    await DataSeeder.SeedAsync(app.Services);
    startupLogger.LogInformation("Database seeding completed.");
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "An error occurred during database migration/seeding.");
}

// ═══════════════════════════════════════════════════════════════
// RUN
// ═══════════════════════════════════════════════════════════════
app.Run();
