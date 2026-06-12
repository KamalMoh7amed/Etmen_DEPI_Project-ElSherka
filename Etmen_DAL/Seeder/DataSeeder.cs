using Etmen_DAL.DbContext;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Etmen_DAL.Seed
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EtmenDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1️⃣ Seed Roles
            string[] roleNames = { "Patient", "Doctor", "Admin", "CrisisAdmin", "HospitalStaff" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // 2️⃣ Seed Admin User
            var adminEmail = "admin@etmen.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRolesAsync(adminUser, new[] { "Admin", "CrisisAdmin" });
                }
            }

            // 2.5️⃣ Seed Hospital Staff User
            var staffEmail = "staff@etmen.com";
            if (await userManager.FindByEmailAsync(staffEmail) == null)
            {
                var staffUser = new ApplicationUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    FirstName = "Hospital",
                    LastName = "Staff",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(staffUser, "Staff@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(staffUser, "HospitalStaff");
                }
            }

            // 3️⃣ Seed Default Crisis Configuration
            if (!await context.CrisisConfigurations.AnyAsync())
            {
                context.CrisisConfigurations.Add(new CrisisConfiguration
                {
                    CrisisName = "Default Mode",
                    CrisisType = CrisisType.Viral,
                    SystemMode = SystemMode.Normal,
                    IsActive = false,
                    StartDate = DateTime.UtcNow,
                    EmergencyThreshold = 0.7m,
                    HighRiskThreshold = 0.5m,
                    MediumRiskThreshold = 0.3m
                });
                await context.SaveChangesAsync();
            }

            // 3.5️⃣ Seed Hantavirus Crisis Configuration
            if (!await context.CrisisConfigurations.AnyAsync(c => c.CrisisName.Contains("هانتا") || c.CrisisName.Contains("Hanta")))
            {
                var hanta = new CrisisConfiguration
                {
                    CrisisName = "وباء فيروس هانتا الرئوي (Hantavirus)",
                    CrisisType = CrisisType.Viral,
                    SystemMode = SystemMode.Crisis,
                    IsActive = true,
                    Description = "متلازمة الرئة بفيروس هانتا (Hantavirus Pulmonary Syndrome) - مرض فيروسي حاد ينتقل عن طريق مخلفات القوارض ويسبب فشلاً تنفسياً حاداً وضيق تنفس حاد.",
                    StartDate = DateTime.UtcNow,
                    EmergencyThreshold = 0.75m,
                    HighRiskThreshold = 0.55m,
                    MediumRiskThreshold = 0.35m,
                    SymptomWeights = new List<SymptomWeight>
                    {
                        new SymptomWeight { SymptomName = "ضيق تنفس حاد", Weight = 0.90m, IsEmergencySymptom = true },
                        new SymptomWeight { SymptomName = "حمى شديدة (ارتفاع الحرارة)", Weight = 0.85m, IsEmergencySymptom = true },
                        new SymptomWeight { SymptomName = "سعال جاف", Weight = 0.70m, IsEmergencySymptom = false },
                        new SymptomWeight { SymptomName = "آلام حادة بالعضلات", Weight = 0.60m, IsEmergencySymptom = false },
                        new SymptomWeight { SymptomName = "إرهاق شديد وصداع", Weight = 0.50m, IsEmergencySymptom = false },
                        new SymptomWeight { SymptomName = "غثيان أو قيء", Weight = 0.45m, IsEmergencySymptom = false }
                    }
                };

                // Deactivate other active configurations
                var activeCrises = await context.CrisisConfigurations.Where(c => c.IsActive).ToListAsync();
                foreach (var active in activeCrises)
                {
                    active.IsActive = false;
                }

                context.CrisisConfigurations.Add(hanta);
                await context.SaveChangesAsync();
            }

            // 4️⃣ Programmatic Bulk Seeder for Telemetry and GPS maps
            if (!await context.HealthcareProviders.AnyAsync(p => p.Name.Contains("مستشفى") || p.Name.Contains("عيادة")))
            {
                var governorates = new[]
                {
                    new { Name = "القاهرة", City = "القاهرة", LatMin = 30.00, LatMax = 30.15, LngMin = 31.15, LngMax = 31.35 },
                    new { Name = "الجيزة", City = "الجيزة", LatMin = 29.95, LatMax = 30.08, LngMin = 31.10, LngMax = 31.25 },
                    new { Name = "الأسكندرية", City = "الأسكندرية", LatMin = 31.15, LatMax = 31.30, LngMin = 29.85, LngMax = 30.05 },
                    new { Name = "الشرقية", City = "الزقازيق", LatMin = 30.50, LatMax = 30.70, LngMin = 31.40, LngMax = 31.60 }
                };

                var random = new Random(42);

                var providers = new List<HealthcareProvider>();
                
                // Seed 50 Hospitals
                for (int i = 1; i <= 50; i++)
                {
                    var gov = governorates[random.Next(governorates.Length)];
                    var lat = (decimal)(gov.LatMin + (gov.LatMax - gov.LatMin) * random.NextDouble());
                    var lng = (decimal)(gov.LngMin + (gov.LngMax - gov.LngMin) * random.NextDouble());
                    providers.Add(new HealthcareProvider
                    {
                        Name = $"مستشفى {GetHospitalName(i)} التخصصي",
                        Type = "Hospital",
                        Latitude = lat,
                        Longitude = lng,
                        Address = $"{gov.Name} - {gov.City}",
                        Phone = $"010{random.Next(10000000, 99999999)}",
                        AvailableBeds = random.Next(10, 150),
                        IsEmergencyCenter = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Seed 100 Clinics
                for (int i = 1; i <= 100; i++)
                {
                    var gov = governorates[random.Next(governorates.Length)];
                    var lat = (decimal)(gov.LatMin + (gov.LatMax - gov.LatMin) * random.NextDouble());
                    var lng = (decimal)(gov.LngMin + (gov.LngMax - gov.LngMin) * random.NextDouble());
                    providers.Add(new HealthcareProvider
                    {
                        Name = $"عيادة {GetClinicName(i)} الطبية",
                        Type = "Clinic",
                        Latitude = lat,
                        Longitude = lng,
                        Address = $"{gov.Name} - {gov.City}",
                        Phone = $"011{random.Next(10000000, 99999999)}",
                        AvailableBeds = 0,
                        IsEmergencyCenter = false,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await context.HealthcareProviders.AddRangeAsync(providers);
                await context.SaveChangesAsync();

                // Seed 200 Doctors
                var doctorUsers = new List<ApplicationUser>();
                var doctorProfiles = new List<DoctorProfile>();
                var doctorRoles = new List<IdentityUserRole<string>>();

                var doctorRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Doctor");
                var doctorRoleId = doctorRole?.Id ?? Guid.NewGuid().ToString();

                var specializations = new[] { "قلب وأوعية دموية", "أطفال", "أسنان", "عظام", "جلدية", "مخ وأعصاب", "رمد", "باطنة", "جراحة عامة", "نساء وتوليد" };
                var docFirstNames = new[] { "محمد", "أحمد", "محمود", "علي", "عمر", "خالد", "يوسف", "إبراهيم", "طارق", "حسام" };
                var docLastNames = new[] { "الشرقاوي", "سالم", "عبد العزيز", "عوض", "المنشاوي", "الحداد", "حسني", "سليمان", "شاكر", "سعيد" };

                var passwordHasher = new PasswordHasher<ApplicationUser>();
                string docHashedPassword = passwordHasher.HashPassword(null!, "Doc@123");

                var clinicProviders = providers.Where(p => p.Type == "Clinic").ToList();

                for (int i = 1; i <= 200; i++)
                {
                    var docId = Guid.NewGuid().ToString();
                    var email = $"doctor{i}@etmen.com";
                    var firstName = docFirstNames[random.Next(docFirstNames.Length)];
                    var lastName = docLastNames[random.Next(docLastNames.Length)];

                    var user = new ApplicationUser
                    {
                        Id = docId,
                        UserName = email,
                        Email = email,
                        NormalizedEmail = email.ToUpperInvariant(),
                        NormalizedUserName = email.ToUpperInvariant(),
                        FirstName = firstName,
                        LastName = lastName,
                        EmailConfirmed = true,
                        IsActive = true,
                        PasswordHash = docHashedPassword,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow
                    };
                    doctorUsers.Add(user);

                    doctorRoles.Add(new IdentityUserRole<string> { UserId = docId, RoleId = doctorRoleId });

                    string onboardingJson = string.Empty;
                    decimal? consultationFee = random.Next(100, 500);
                    int exp = random.Next(3, 25);
                    string specialization = specializations[random.Next(specializations.Length)];

                    if (i <= clinicProviders.Count)
                    {
                        var clinic = clinicProviders[i - 1];
                        onboardingJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            EntityName = clinic.Name,
                            EntityType = clinic.Type,
                            BranchArabicName = clinic.Name,
                            City = clinic.Address?.Split('-').FirstOrDefault()?.Trim() ?? "القاهرة",
                            Area = clinic.Address?.Split('-').LastOrDefault()?.Trim() ?? "القاهرة",
                            BranchMobile = clinic.Phone,
                            Latitude = clinic.Latitude,
                            Longitude = clinic.Longitude,
                            HealthcareProviderId = clinic.Id
                        });
                    }
                    else
                    {
                        var gov = governorates[random.Next(governorates.Length)];
                        var lat = (decimal)(gov.LatMin + (gov.LatMax - gov.LatMin) * random.NextDouble());
                        var lng = (decimal)(gov.LngMin + (gov.LngMax - gov.LngMin) * random.NextDouble());
                        onboardingJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            EntityName = $"عيادة د. {firstName} {lastName}",
                            EntityType = "Clinic",
                            City = gov.Name,
                            Area = gov.City,
                            Latitude = lat,
                            Longitude = lng
                        });
                    }

                    var profile = new DoctorProfile
                    {
                        ApplicationUserId = docId,
                        FullName = $"د. {firstName} {lastName}",
                        Specialization = specialization,
                        LicenseNumber = $"LIC{random.Next(100000, 999999)}",
                        YearsOfExperience = exp,
                        ConsultationFee = consultationFee,
                        IsAvailable = true,
                        IsOnboarded = true,
                        OnboardingDataJson = onboardingJson,
                        CreatedAt = DateTime.UtcNow
                    };
                    doctorProfiles.Add(profile);
                }

                await context.Users.AddRangeAsync(doctorUsers);
                await context.UserRoles.AddRangeAsync(doctorRoles);
                await context.DoctorProfiles.AddRangeAsync(doctorProfiles);
                await context.SaveChangesAsync();

                // Seed 500 Patients
                var patientUsers = new List<ApplicationUser>();
                var patientProfiles = new List<PatientProfile>();
                var patientRoles = new List<IdentityUserRole<string>>();

                var patientRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Patient");
                var patientRoleId = patientRole?.Id ?? Guid.NewGuid().ToString();

                var patFirstNames = new[] { "أحمد", "سامي", "رائد", "هشام", "كريم", "خالد", "عبد الله", "حامد", "نادر", "مصطفى" };
                var patLastNames = new[] { "سالم", "جلال", "حسن", "منصور", "شاهين", "رزق", "الخولي", "البغدادي", "حسين", "فوزي" };

                string patHashedPassword = passwordHasher.HashPassword(null!, "Pat@123");

                for (int i = 1; i <= 500; i++)
                {
                    var patId = Guid.NewGuid().ToString();
                    var email = $"patient{i}@etmen.com";
                    var firstName = patFirstNames[random.Next(patFirstNames.Length)];
                    var lastName = patLastNames[random.Next(patLastNames.Length)];

                    var gov = governorates[random.Next(governorates.Length)];
                    var lat = (decimal)(gov.LatMin + (gov.LatMax - gov.LatMin) * random.NextDouble());
                    var lng = (decimal)(gov.LngMin + (gov.LngMax - gov.LngMin) * random.NextDouble());

                    var user = new ApplicationUser
                    {
                        Id = patId,
                        UserName = email,
                        Email = email,
                        NormalizedEmail = email.ToUpperInvariant(),
                        NormalizedUserName = email.ToUpperInvariant(),
                        FirstName = firstName,
                        LastName = lastName,
                        EmailConfirmed = true,
                        IsActive = true,
                        PasswordHash = patHashedPassword,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow
                    };
                    patientUsers.Add(user);

                    patientRoles.Add(new IdentityUserRole<string> { UserId = patId, RoleId = patientRoleId });

                    var profile = new PatientProfile
                    {
                        ApplicationUserId = patId,
                        FullName = $"{firstName} {lastName}",
                        DateOfBirth = DateTime.Today.AddYears(-random.Next(18, 70)).AddDays(-random.Next(0, 365)),
                        Gender = random.Next(2) == 0 ? "Male" : "Female",
                        Height = random.Next(150, 195),
                        Weight = random.Next(50, 110),
                        BloodType = new[] { "A+", "B+", "O+", "AB+", "A-", "B-", "O-" }[random.Next(7)],
                        HasChronicDiseases = random.Next(4) == 0,
                        Latitude = lat,
                        Longitude = lng,
                        CreatedAt = DateTime.UtcNow
                    };
                    patientProfiles.Add(profile);
                }

                await context.Users.AddRangeAsync(patientUsers);
                await context.UserRoles.AddRangeAsync(patientRoles);
                await context.PatientProfiles.AddRangeAsync(patientProfiles);
                await context.SaveChangesAsync();

                // Seed 100 Critical cases (Emergency requests) for the first 100 Patients
                var riskAssessments = new List<RiskAssessment>();
                var emergencyRequests = new List<EmergencyRequest>();

                var emergencyTypes = new[] { "Cardiovascular", "Respiratory", "Trauma", "Neurological", "Infectious" };
                var emergencyDescriptions = new[] 
                { 
                    "آلام حادة وشديدة في الصدر مع ضيق تنفس", 
                    "ضيق تنفس حاد وانخفاض نسبة الأكسجين", 
                    "نزيف مستمر نتيجة كسر مضاعف", 
                    "شبه غيبوبة مع عدم القدرة على الحركة", 
                    "ارتفاع شديد في درجة الحرارة وصعوبة في البلع" 
                };

                for (int i = 0; i < 100; i++)
                {
                    var patientProfile = patientProfiles[i];
                    var symptoms = "ChestPain,Dyspnea,HighFever,LossOfConsciousness";
                    var recommendations = "[\"اتصل بالإسعاف فوراً\",\"استلقِ على ظهرك\",\"لا تبذل مجهوداً\"]";

                    var risk = new RiskAssessment
                    {
                        PatientProfileId = patientProfile.Id,
                        AssessmentDate = DateTime.UtcNow.AddMinutes(-random.Next(10, 120)),
                        RiskScore = 0.85m + (decimal)(random.NextDouble() * 0.1),
                        RiskLevel = RiskLevel.Critical,
                        Symptoms = symptoms,
                        RecommendationsJson = recommendations,
                        IsEmergency = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    riskAssessments.Add(risk);
                }

                await context.RiskAssessments.AddRangeAsync(riskAssessments);
                await context.SaveChangesAsync();

                for (int i = 0; i < 100; i++)
                {
                    var patientProfile = patientProfiles[i];
                    var risk = riskAssessments[i];
                    var reqIdx = random.Next(emergencyTypes.Length);

                    var request = new EmergencyRequest
                    {
                        PatientProfileId = patientProfile.Id,
                        RiskAssessmentId = risk.Id,
                        Status = EmergencyRequestStatus.Pending,
                        EmergencyType = emergencyTypes[reqIdx],
                        Description = emergencyDescriptions[reqIdx],
                        Latitude = patientProfile.Latitude,
                        Longitude = patientProfile.Longitude,
                        PriorityScore = random.Next(80, 100),
                        RequestedAt = DateTime.UtcNow.AddMinutes(-random.Next(5, 60)),
                        IsAutoGenerated = true
                    };
                    emergencyRequests.Add(request);
                }

                await context.EmergencyRequests.AddRangeAsync(emergencyRequests);
                await context.SaveChangesAsync();
            }

            // 5️⃣ Seed Outbreak Zones if empty
            if (!await context.OutbreakZones.AnyAsync())
            {
                var defaultCrisis = await context.CrisisConfigurations.FirstOrDefaultAsync();
                if (defaultCrisis != null)
                {
                    var outbreakZones = new List<OutbreakZone>
                    {
                        new OutbreakZone
                        {
                            CrisisConfigurationId = defaultCrisis.Id,
                            ZoneName = "بؤرة القاهرة الكبرى",
                            CenterLatitude = 30.0500m,
                            CenterLongitude = 31.2500m,
                            RadiusInKm = 15.0m,
                            RiskLevel = 3, // Emergency
                            CreatedAt = DateTime.UtcNow
                        },
                        new OutbreakZone
                        {
                            CrisisConfigurationId = defaultCrisis.Id,
                            ZoneName = "بؤرة الإسكندرية الساحلية",
                            CenterLatitude = 31.2200m,
                            CenterLongitude = 29.9500m,
                            RadiusInKm = 12.0m,
                            RiskLevel = 2, // High Risk
                            CreatedAt = DateTime.UtcNow
                        },
                        new OutbreakZone
                        {
                            CrisisConfigurationId = defaultCrisis.Id,
                            ZoneName = "بؤرة وسط الدلتا (الشرقية)",
                            CenterLatitude = 30.5800m,
                            CenterLongitude = 31.5000m,
                            RadiusInKm = 20.0m,
                            RiskLevel = 1, // Medium Risk
                            CreatedAt = DateTime.UtcNow
                        }
                    };
                    await context.OutbreakZones.AddRangeAsync(outbreakZones);
                    await context.SaveChangesAsync();
                }
            }
        }

        private static string GetHospitalName(int i)
        {
            var names = new[] { "السلام", "النور", "الأمل", "الحياة", "النيل", "دار الفؤاد", "الشروق", "المستقبل", "الصفوة", "الشفاء" };
            return names[(i - 1) % names.Length] + " " + ((i - 1) / names.Length + 1);
        }

        private static string GetClinicName(int i)
        {
            var names = new[] { "الرحمة", "التوحيد", "الياسمين", "الزهور", "طيبة", "ابن سينا", "الفارابي", "النخبة", "الهدى", "الرضا" };
            return names[(i - 1) % names.Length] + " " + ((i - 1) / names.Length + 1);
        }
        }
    }
