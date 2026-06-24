using Etmen_BLL.Helpers;
using Etmen_BLL.Mapping;
using Etmen_BLL.Repositories.IServices;
using Etmen_BLL.Repositories.Services;
using Etmen_DAL.DbContext;
using Etmen_DAL.Repositories.Implementations;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Mapster;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Etmen_PL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // 1. DATABASE CONTEXT
            services.AddDbContext<EtmenDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql =>
                    {
                        sql.MigrationsAssembly(typeof(EtmenDbContext).Assembly.FullName);
                        sql.CommandTimeout(60);
                        sql.EnableRetryOnFailure(3);
                    }
                )
            );
            // 2. IDENTITY
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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
                    configuration.GetValue<int>("Security:LockoutDurationInMinutes", 15));
                options.Lockout.MaxFailedAccessAttempts = configuration.GetValue<int>("Security:MaxLoginAttempts", 5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<EtmenDbContext>()
            .AddDefaultTokenProviders();

            //  Cookie authentication 
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";

                options.Cookie.Name = "EtmenAuthCookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;

                options.ExpireTimeSpan = TimeSpan.FromDays(
                    configuration.GetValue<int>("Security:CookieExpirationDays", 7));
                options.SlidingExpiration = true;
            });

            // 3. DATA ACCESS (Unit of Work)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 4. HTTP CONTEXT ACCESSOR (required for dynamic base URL in AuthService)
            services.AddHttpContextAccessor();

            // 5. BUSINESS LOGIC LAYER — ALL SERVICES

            // Mail Services (must be registered first — other services depend on them)
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPdfReportService, PdfReportService>();

            // Safe background task queue and hosted service
            services.AddSingleton<IBackgroundTaskQueue>(new BackgroundTaskQueue(100));
            services.AddHostedService<QueuedHostedService>();

            // Appointment reminder background service (checks every 30 minutes)
            services.AddHostedService<AppointmentReminderHostedService>();

            // Auth
            services.AddScoped<IAuthService, AuthService>();

            // Patient
            services.AddScoped<IPatientService, PatientService>();
            services.AddScoped<IMedicalRecordService, MedicalRecordService>();

            // Doctor
            services.AddScoped<IDoctorService, DoctorService>();

            // Appointments
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<INearbyService, NearbyService>();

            // Health Monitoring
            services.AddScoped<IRiskService, RiskService>();
            services.AddScoped<ILabService, LabService>();
            services.AddScoped<IAlertService, AlertService>();

            // Communications
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IReviewService, ReviewService>();

            services.Configure<ChatbotApiOptions>(
                    configuration.GetSection("ChatbotApi"));

            services.AddHttpClient<IChatbotService, ChatbotService>()
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

            // Family
            services.AddScoped<IFamilyService, FamilyService>();

            // Emergency
            services.AddScoped<IEmergencyService, EmergencyService>();
            services.AddScoped<IHospitalStaffService, HospitalStaffService>();
            services.AddScoped<ICriticalCareEscalationService, CriticalCareEscalationService>();
            services.AddScoped<ICriticalIntelligenceService, CriticalIntelligenceService>();

            // Crisis
            services.AddScoped<ICrisisService, CrisisService>();
            services.AddScoped<ICrisisRiskEngineService, CrisisRiskEngineService>();

            // Admin
            services.AddScoped<IAdminService, AdminService>();

            // 5. MAPSTER
            var mapsterConfig = TypeAdapterConfig.GlobalSettings;
            mapsterConfig.Scan(typeof(BLLMappingProfile).Assembly);   
            mapsterConfig.Compile();                                    

            services.AddSingleton(mapsterConfig);

            // 6. MVC
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add<Etmen_PL.Filters.DoctorOnboardingFilter>();
                options.Filters.Add<Etmen_PL.Filters.PatientProfileFilter>();
                options.Filters.Add<Etmen_PL.Filters.MaintenanceFilter>();
            })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler =
                        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.WriteIndented = environment.IsDevelopment();
                });

            services.AddRazorPages();
            services.AddSignalR();

            // 7. SESSION
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(
                    configuration.GetValue<int>("Security:SessionTimeoutMinutes", 60));
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.Name = "EtmenSession";
            });

            // 8. CACHING
            services.AddMemoryCache();

         
            // 9. HTTP CLIENT (for external APIs: OCR, AI, etc.)
            services.AddHttpClient();

            // 10. CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                    policy
                        .WithOrigins("https://localhost:5001", "https://localhost:7001")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });

            // 11. ANTI-FORGERY
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "EtmenAntiForgery";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // 12. FILE UPLOAD LIMIT (10 MB)
            services.Configure<FormOptions>(options =>
            {
                int maxMb = configuration.GetValue<int>("FileUpload:MaxFileSizeInMB", 10);
                options.MultipartBodyLengthLimit = maxMb * 1024 * 1024;
            });

            return services;
        }
    }
}
