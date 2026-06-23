using Microsoft.AspNetCore.Identity;
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
            ApplicationUser adminUser;
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                adminUser = new ApplicationUser
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

            // 3️⃣ Seed Hospital Staff User
            var staffEmail = "staff@etmen.com";
            ApplicationUser staffUser = null;
            if (await userManager.FindByEmailAsync(staffEmail) == null)
            {
                staffUser = new ApplicationUser
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
            else
            {
                staffUser = await userManager.FindByEmailAsync(staffEmail);
            }

            // 4️⃣ Seed Doctor User
            var doctorEmail = "doctor1@etmen.com";
            ApplicationUser doctorUser = null;
            if (await userManager.FindByEmailAsync(doctorEmail) == null)
            {
                doctorUser = new ApplicationUser
                {
                    UserName = doctorEmail,
                    Email = doctorEmail,
                    FirstName = "حازم",
                    LastName = "الببلاوي",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(doctorUser, "Doctor@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(doctorUser, "Doctor");
                }
            }
            else
            {
                doctorUser = await userManager.FindByEmailAsync(doctorEmail);
            }

            // 5️⃣ Seed Patient User
            var patientEmail = "patient1@etmen.com";
            ApplicationUser patientUser = null;
            if (await userManager.FindByEmailAsync(patientEmail) == null)
            {
                patientUser = new ApplicationUser
                {
                    UserName = patientEmail,
                    Email = patientEmail,
                    FirstName = "أحمد",
                    LastName = "محمد علي",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(patientUser, "Patient@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(patientUser, "Patient");
                }
            }
            else
            {
                patientUser = await userManager.FindByEmailAsync(patientEmail);
            }

            // 6️⃣ Seed Exactly ONE default Hospital and Clinic
            HealthcareProvider defaultHospital;
            HealthcareProvider defaultClinic;

            if (!await context.HealthcareProviders.AnyAsync())
            {
                defaultHospital = new HealthcareProvider
                {
                    Name = "مستشفى طوارئ السلام",
                    Type = "Hospital",
                    Latitude = 30.0444m,
                    Longitude = 31.2357m,
                    Address = "القاهرة - وسط المدينة",
                    Phone = "01012345678",
                    AvailableBeds = 50,
                    BedCapacity = 100,
                    AmbulanceCapacity = 5,
                    AvailableAmbulances = 5,
                    IsEmergencyCenter = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                defaultClinic = new HealthcareProvider
                {
                    Name = "عيادة الرحمة الطبية",
                    Type = "Clinic",
                    Latitude = 30.0500m,
                    Longitude = 31.2400m,
                    Address = "القاهرة - الحي السكني",
                    Phone = "01098765432",
                    IsEmergencyCenter = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.HealthcareProviders.AddAsync(defaultHospital);
                await context.HealthcareProviders.AddAsync(defaultClinic);
                await context.SaveChangesAsync();
            }
            else
            {
                defaultHospital = await context.HealthcareProviders.FirstOrDefaultAsync(h => h.Name == "مستشفى طوارئ السلام") ?? await context.HealthcareProviders.FirstAsync();
                defaultClinic = await context.HealthcareProviders.FirstOrDefaultAsync(h => h.Name == "عيادة الرحمة الطبية") ?? await context.HealthcareProviders.FirstAsync();
            }

            // 7️⃣ Link staff@etmen.com to the Hospital via StaffProfile
            if (staffUser != null && !await context.StaffProfiles.AnyAsync(s => s.ApplicationUserId == staffUser.Id))
            {
                var staffProfile = new StaffProfile
                {
                    ApplicationUserId = staffUser.Id,
                    HealthcareProviderId = defaultHospital.Id,
                    RoleType = StaffRoleType.Receptionist,
                    ActiveShift = StaffShiftType.Morning,
                    IsInvitationAccepted = true,
                    JoinedAt = DateTime.UtcNow
                };
                context.StaffProfiles.Add(staffProfile);
            }

            // 8️⃣ Link Doctor to DoctorProfile and DoctorProvider
            if (doctorUser != null && !await context.DoctorProfiles.AnyAsync(d => d.ApplicationUserId == doctorUser.Id))
            {
                var doctorProfile = new DoctorProfile
                {
                    ApplicationUserId = doctorUser.Id,
                    FullName = "د. حازم الببلاوي",
                    Specialization = "طوارئ وعناية مركزة",
                    LicenseNumber = "LIC-12345",
                    YearsOfExperience = 12,
                    Bio = "استشاري طب الطوارئ والحالات الحرجة.",
                    ConsultationFee = 200m,
                    IsAvailable = true,
                    IsOnboarded = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.DoctorProfiles.Add(doctorProfile);
                await context.SaveChangesAsync(); // save to generate doctorProfile.Id

                var doctorProvider = new DoctorProvider
                {
                    DoctorProfileId = doctorProfile.Id,
                    HealthcareProviderId = defaultHospital.Id,
                    IsEmergencyDoctor = true,
                    AffiliationRole = "Consultant"
                };

                var doctorProvider2 = new DoctorProvider
                {
                    DoctorProfileId = doctorProfile.Id,
                    HealthcareProviderId = defaultClinic.Id,
                    IsEmergencyDoctor = false,
                    AffiliationRole = "Clinic Doctor"
                };

                context.DoctorProviders.Add(doctorProvider);
                context.DoctorProviders.Add(doctorProvider2);
            }

            // 9️⃣ Link Patient to PatientProfile
            if (patientUser != null && !await context.PatientProfiles.AnyAsync(p => p.ApplicationUserId == patientUser.Id))
            {
                var patientProfile = new PatientProfile
                {
                    ApplicationUserId = patientUser.Id,
                    FullName = "أحمد محمد علي",
                    DateOfBirth = DateTime.UtcNow.AddYears(-30),
                    Gender = "Male",
                    Height = 175m,
                    Weight = 70m,
                    ActivityLevel = PhysicalActivityLevel.Moderate,
                    BloodType = "A+",
                    HasChronicDiseases = false,
                    Latitude = 30.0450m,
                    Longitude = 31.2360m,
                    CreatedAt = DateTime.UtcNow
                };
                context.PatientProfiles.Add(patientProfile);
            }

            await context.SaveChangesAsync();

            // 🔟 Seed Default Crisis Configuration
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

            // 11 Seed Hantavirus Crisis Configuration
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

            // 12 Seed Outbreak Zones
            if (!await context.OutbreakZones.AnyAsync())
            {
                var defaultCrisis = await context.CrisisConfigurations.FirstOrDefaultAsync(c => c.IsActive);
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
                        }
                    };
                    await context.OutbreakZones.AddRangeAsync(outbreakZones);
                    await context.SaveChangesAsync();
                }
            }

            // 13 Seed custom bulk data requested by user (250 providers, 70 doctors, 600 emergency requests)
            var hasGovernorateSeed = await context.HealthcareProviders.AnyAsync(h => h.Address != null && h.Address.Contains("محافظة"));
            var hasDefaultNames = await context.Users.AnyAsync(u => (u.FirstName == "مريض" && u.LastName.StartsWith("افتراضي")) || (u.FirstName == "طبيب" && u.LastName.StartsWith("تجريبي")));
            if (await context.HealthcareProviders.CountAsync() < 250 || !hasGovernorateSeed || hasDefaultNames)
            {
                var random = new Random();
                var defaultPasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), "Etmen@123");

                // Clean up previous bulk seeded users and data to recreate them with the 27 governorates
                var bulkUsers = await context.Users.Where(u => u.Email != null && (u.Email.StartsWith("patient_bulk_") || u.Email.StartsWith("doctor_bulk_"))).ToListAsync();
                var bulkUserIds = bulkUsers.Select(u => u.Id).ToList();

                // Delete dependencies
                await context.Appointments.ExecuteDeleteAsync();
                await context.AvailableSlots.ExecuteDeleteAsync();
                await context.Reviews.ExecuteDeleteAsync();
                await context.EmergencyRequests.ExecuteDeleteAsync();
                await context.DoctorProviders.ExecuteDeleteAsync();
                await context.MedicalRecords.ExecuteDeleteAsync();
                await context.RiskAssessments.ExecuteDeleteAsync();
                await context.LabResults.ExecuteDeleteAsync();

                if (bulkUserIds.Any())
                {
                    await context.Notifications.Where(n => bulkUserIds.Contains(n.UserId)).ExecuteDeleteAsync();
                    await context.UserRoles.Where(ur => bulkUserIds.Contains(ur.UserId)).ExecuteDeleteAsync();
                    await context.DoctorProfiles.Where(d => bulkUserIds.Contains(d.ApplicationUserId)).ExecuteDeleteAsync();
                    await context.PatientProfiles.Where(p => bulkUserIds.Contains(p.ApplicationUserId)).ExecuteDeleteAsync();
                    await context.Users.Where(u => bulkUserIds.Contains(u.Id)).ExecuteDeleteAsync();
                }

                await context.HealthcareProviders.Where(h => h.Name.Contains("السلامة والرحمة")).ExecuteDeleteAsync();
                await context.SaveChangesAsync();

                // Define 27 Egyptian governorates coordinates
                var governorates = new[]
                {
                    new { Name = "القاهرة", Lat = 30.0444m, Lng = 31.2357m },
                    new { Name = "الجيزة", Lat = 30.0131m, Lng = 31.2089m },
                    new { Name = "الإسكندرية", Lat = 31.2001m, Lng = 29.9187m },
                    new { Name = "الدقهلية", Lat = 31.0409m, Lng = 31.3785m },
                    new { Name = "البحر الأحمر", Lat = 27.2579m, Lng = 33.8116m },
                    new { Name = "البحيرة", Lat = 31.0364m, Lng = 30.4688m },
                    new { Name = "الفيوم", Lat = 29.3084m, Lng = 30.8428m },
                    new { Name = "الغربية", Lat = 30.7865m, Lng = 31.0004m },
                    new { Name = "الإسماعيلية", Lat = 30.6044m, Lng = 32.2723m },
                    new { Name = "المنوفية", Lat = 30.5574m, Lng = 31.0097m },
                    new { Name = "المنيا", Lat = 28.0871m, Lng = 30.7618m },
                    new { Name = "القليوبية", Lat = 30.4591m, Lng = 31.1858m },
                    new { Name = "الوادي الجديد", Lat = 25.4390m, Lng = 30.5586m },
                    new { Name = "الشرقية", Lat = 30.5877m, Lng = 31.5019m },
                    new { Name = "السويس", Lat = 29.9668m, Lng = 32.5498m },
                    new { Name = "أسوان", Lat = 24.0889m, Lng = 32.8998m },
                    new { Name = "أسيوط", Lat = 27.1783m, Lng = 31.1859m },
                    new { Name = "بني سويف", Lat = 29.0744m, Lng = 31.0978m },
                    new { Name = "بورسعيد", Lat = 31.2653m, Lng = 32.3019m },
                    new { Name = "دمياط", Lat = 31.4175m, Lng = 31.8144m },
                    new { Name = "جنوب سيناء", Lat = 28.2364m, Lng = 33.6254m },
                    new { Name = "كفر الشيخ", Lat = 31.1107m, Lng = 30.9388m },
                    new { Name = "مطروح", Lat = 31.3543m, Lng = 27.2373m },
                    new { Name = "قنا", Lat = 26.1551m, Lng = 32.7160m },
                    new { Name = "شمال سيناء", Lat = 31.1321m, Lng = 33.7984m },
                    new { Name = "سوهاج", Lat = 26.5591m, Lng = 31.6957m },
                    new { Name = "الأقصر", Lat = 25.6872m, Lng = 32.6396m }
                };

                // --- 1. Seed 250 Providers ---
                var providersList = new List<HealthcareProvider>();
                var providerTypes = new string[] { "Hospital", "Clinic", "HealthCenter" };
                var providerNames = new string[] { "مستشفى", "عيادة", "مركز طبي" };

                for (int i = 1; i <= 250; i++)
                {
                    var type = providerTypes[random.Next(providerTypes.Length)];
                    var namePrefix = providerNames[random.Next(providerNames.Length)];
                    var gov = governorates[(i - 1) % governorates.Length];
                    var name = $"{namePrefix} السلامة والرحمة {i} - {gov.Name}";

                    var isEmergency = type == "Hospital" || random.Next(0, 3) == 0;
                    int? bedCapacity = null;
                    int? availableBeds = null;
                    int? ambulanceCapacity = null;
                    int? availableAmbulances = null;

                    if (isEmergency)
                    {
                        bedCapacity = random.Next(100, 201);
                        availableBeds = bedCapacity;
                        ambulanceCapacity = random.Next(15, 41);
                        availableAmbulances = ambulanceCapacity;
                    }

                    providersList.Add(new HealthcareProvider
                    {
                        Name = name,
                        Type = type,
                        Latitude = gov.Lat + (decimal)(random.NextDouble() - 0.5) * 0.05m,
                        Longitude = gov.Lng + (decimal)(random.NextDouble() - 0.5) * 0.05m,
                        Address = $"محافظة {gov.Name} - شارع السلام رقم {i}",
                        Phone = $"010{random.Next(10000000, 99999999)}",
                        BedCapacity = bedCapacity,
                        AvailableBeds = availableBeds,
                        AmbulanceCapacity = ambulanceCapacity,
                        AvailableAmbulances = availableAmbulances,
                        IsEmergencyCenter = isEmergency,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await context.HealthcareProviders.AddRangeAsync(providersList);
                await context.SaveChangesAsync();

                // --- 2. Seed 100 Patients (for emergency requests) ---
                var maleNames = new[] { "محمد", "أحمد", "يوسف", "عمر", "محمود", "علي", "مصطفى", "خالد", "إبراهيم", "طارق", "كريم", "عمرو", "شريف", "هاني", "وائل", "سامح", "ياسر", "أمجد", "سعيد", "أيمن", "حسن", "حسين", "عادل", "ماجد", "ممدوح", "هشام", "زياد", "رائد", "تامر", "جمال" };
                var femaleNames = new[] { "منى", "سارة", "فاطمة", "رنا", "مي", "نور", "أمل", "مريم", "سلمى", "ياسمين", "ندى", "آية", "نهى", "دينا", "شيماء", "رانيا", "هبة", "إيمان", "مروة", "علا" };
                var lastNames = new[] { "الشافعي", "المصري", "المهدي", "الحداد", "الرشيدي", "منصور", "سليم", "زايد", "بدوي", "حسن", "عثمان", "غنيم", "زكي", "كامل", "حسني", "عبد الرحمن", "سليمان", "شحاتة", "السيد", "عطية", "الشناوي", "عبد العزيز", "عوض", "فوزي", "راضي", "سالم", "عيسى", "الخطيب", "نافع", "البارودي" };

                var patientNames = new List<(string FirstName, string LastName, string Gender)>();
                for (int i = 1; i <= 100; i++)
                {
                    var isMale = random.Next(0, 2) == 1;
                    var genderStr = isMale ? "Male" : "Female";
                    var firstName = isMale ? maleNames[random.Next(maleNames.Length)] : femaleNames[random.Next(femaleNames.Length)];
                    var lastName = lastNames[random.Next(lastNames.Length)];
                    patientNames.Add((firstName, lastName, genderStr));
                }

                var patientUsers = new List<ApplicationUser>();
                var patientRole = await roleManager.FindByNameAsync("Patient");
                for (int i = 1; i <= 100; i++)
                {
                    patientUsers.Add(new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = $"patient_bulk_{i}@etmen.com",
                        Email = $"patient_bulk_{i}@etmen.com",
                        FirstName = patientNames[i - 1].FirstName,
                        LastName = patientNames[i - 1].LastName,
                        EmailConfirmed = true,
                        IsActive = true,
                        PasswordHash = defaultPasswordHash
                    });
                }
                await context.Users.AddRangeAsync(patientUsers);
                await context.SaveChangesAsync();

                var patientRoleList = new List<IdentityUserRole<string>>();
                foreach (var user in patientUsers)
                {
                    patientRoleList.Add(new IdentityUserRole<string>
                    {
                        UserId = user.Id,
                        RoleId = patientRole.Id
                    });
                }
                await context.UserRoles.AddRangeAsync(patientRoleList);
                await context.SaveChangesAsync();

                var patientProfilesList = new List<PatientProfile>();
                for (int i = 0; i < 100; i++)
                {
                    var gov = governorates[i % governorates.Length];
                    patientProfilesList.Add(new PatientProfile
                    {
                        ApplicationUserId = patientUsers[i].Id,
                        FullName = $"{patientNames[i].FirstName} {patientNames[i].LastName}",
                        DateOfBirth = DateTime.UtcNow.AddYears(-random.Next(18, 75)),
                        Gender = patientNames[i].Gender,
                        Height = random.Next(150, 195),
                        Weight = random.Next(50, 110),
                        ActivityLevel = (PhysicalActivityLevel)random.Next(0, 4),
                        BloodType = random.Next(0, 8) switch
                        {
                            0 => "A+", 1 => "A-", 2 => "B+", 3 => "B-",
                            4 => "O+", 5 => "O-", 6 => "AB+", _ => "AB-"
                        },
                        HasChronicDiseases = random.Next(0, 3) == 0,
                        Latitude = gov.Lat + (decimal)(random.NextDouble() - 0.5) * 0.04m,
                        Longitude = gov.Lng + (decimal)(random.NextDouble() - 0.5) * 0.04m,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await context.PatientProfiles.AddRangeAsync(patientProfilesList);
                await context.SaveChangesAsync();

                // --- 3. Seed 600 Emergency Requests (حالة خطرة) ---
                var emergencyRequests = new List<EmergencyRequest>();
                var emergencyTypes = new string[] { "نوبة قلبية حادة", "ضيق تنفس شديد", "كسر مضاعف ونزيف", "حالة تسمم حاد", "سكتة دماغية مفاجئة", "انخفاض حاد في ضغط الدم" };
                for (int i = 1; i <= 600; i++)
                {
                    var patient = patientProfilesList[random.Next(patientProfilesList.Count)];
                    var hospital = providersList.Where(p => p.Type == "Hospital" && p.IsEmergencyCenter).OrderBy(x => random.Next()).FirstOrDefault();

                    emergencyRequests.Add(new EmergencyRequest
                    {
                        PatientProfileId = patient.Id,
                        HealthcareProviderId = hospital?.Id,
                        Status = (EmergencyRequestStatus)random.Next(0, 3),
                        EmergencyType = emergencyTypes[random.Next(emergencyTypes.Length)],
                        Description = $"تقرير طوارئ عاجل رقم {i}: يعاني المريض من حالة حرجة ويحتاج نقل فوري لأقرب مستشفى.",
                        Latitude = patient.Latitude,
                        Longitude = patient.Longitude,
                        RequestedAt = DateTime.UtcNow.AddDays(-random.Next(1, 60)),
                        PriorityScore = random.Next(5, 11),
                        DoctorsNotified = true,
                        DoctorsNotifiedAt = DateTime.UtcNow
                    });
                }
                await context.EmergencyRequests.AddRangeAsync(emergencyRequests);
                await context.SaveChangesAsync();

                // --- 4. Seed 70 Doctors ---
                var doctorNames = new List<(string FirstName, string LastName)>();
                for (int i = 1; i <= 70; i++)
                {
                    var isMale = random.Next(0, 2) == 1;
                    var firstName = isMale ? maleNames[random.Next(maleNames.Length)] : femaleNames[random.Next(femaleNames.Length)];
                    var lastName = lastNames[random.Next(lastNames.Length)];
                    doctorNames.Add((firstName, lastName));
                }

                var doctorUsers = new List<ApplicationUser>();
                var doctorRole = await roleManager.FindByNameAsync("Doctor");
                for (int i = 1; i <= 70; i++)
                {
                    doctorUsers.Add(new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = $"doctor_bulk_{i}@etmen.com",
                        Email = $"doctor_bulk_{i}@etmen.com",
                        FirstName = doctorNames[i - 1].FirstName,
                        LastName = doctorNames[i - 1].LastName,
                        EmailConfirmed = true,
                        IsActive = true,
                        PasswordHash = defaultPasswordHash
                    });
                }
                await context.Users.AddRangeAsync(doctorUsers);
                await context.SaveChangesAsync();

                var doctorRoleList = new List<IdentityUserRole<string>>();
                foreach (var user in doctorUsers)
                {
                    doctorRoleList.Add(new IdentityUserRole<string>
                    {
                        UserId = user.Id,
                        RoleId = doctorRole.Id
                    });
                }
                await context.UserRoles.AddRangeAsync(doctorRoleList);
                await context.SaveChangesAsync();

                var doctorProfilesList = new List<DoctorProfile>();
                var specializations = new string[] { "القلب والأوعية الدموية", "طب الأطفال", "الباطنة والجهاز الهضمي", "النساء والتوليد", "العظام والمفاصل", "العيون ورمد", "الجلدية والتناسلية", "الجراحة العامة", "المخ والأعصاب", "الأنف والأذن والحنجرة" };
                for (int i = 0; i < 70; i++)
                {
                    doctorProfilesList.Add(new DoctorProfile
                    {
                        ApplicationUserId = doctorUsers[i].Id,
                        FullName = $"د. {doctorNames[i].FirstName} {doctorNames[i].LastName}",
                        Specialization = specializations[random.Next(specializations.Length)],
                        LicenseNumber = $"LIC-BULK-{20000 + i}",
                        YearsOfExperience = random.Next(3, 28),
                        Bio = "طبيب متميز يقدم استشارات وخدمات تشخيصية وعلاجية عالية الدقة للمرضى في تخصصه الطبي.",
                        ConsultationFee = random.Next(150, 451),
                        IsAvailable = true,
                        IsOnboarded = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await context.DoctorProfiles.AddRangeAsync(doctorProfilesList);
                await context.SaveChangesAsync();

                // --- 5. Generate Reviews for Doctors ---
                var reviewsList = new List<Review>();
                var comments = new string[] { "دكتور ممتاز ومستمع رائع للمريض.", "تشخيص دقيق للغاية والعيادة منظمة.", "طبيب متميز وأنصح بشدة بالتعامل معه.", "المقابلة كانت مريحة ومفيدة للغاية.", "خطة علاج ممتازة وتحسن ملحوظ." };
                foreach (var doc in doctorProfilesList)
                {
                    int reviewCount = random.Next(3, 8);
                    for (int r = 0; r < reviewCount; r++)
                    {
                        var patient = patientProfilesList[random.Next(patientProfilesList.Count)];
                        reviewsList.Add(new Review
                        {
                            PatientProfileId = patient.Id,
                            DoctorProfileId = doc.Id,
                            Rating = random.Next(4, 6),
                            Comment = comments[random.Next(comments.Length)],
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                        });
                    }
                }
                await context.Reviews.AddRangeAsync(reviewsList);
                await context.SaveChangesAsync();

                // --- 6. Seed DoctorProvider Affiliations (5 to 15 doctors per provider) ---
                var doctorProvidersList = new List<DoctorProvider>();
                foreach (var provider in providersList)
                {
                    int docCount = random.Next(5, 16);
                    var shuffledDoctors = doctorProfilesList.OrderBy(x => random.Next()).Take(docCount).ToList();
                    foreach (var doc in shuffledDoctors)
                    {
                        doctorProvidersList.Add(new DoctorProvider
                        {
                            DoctorProfileId = doc.Id,
                            HealthcareProviderId = provider.Id,
                            IsEmergencyDoctor = provider.Type == "Hospital" && random.Next(0, 2) == 1,
                            AffiliationRole = random.Next(0, 3) switch
                            {
                                0 => "المالك / طبيب رئيسي",
                                1 => "استشاري أول",
                                _ => "عضو طاقم طبي"
                            }
                        });
                    }
                }
                await context.DoctorProviders.AddRangeAsync(doctorProvidersList);
                await context.SaveChangesAsync();

                // --- 7. Seed Appointments Slots from today to 2 months ahead ---
                var today = DateTime.Today;
                var slotsList = new List<AvailableSlot>();
                for (int dayOffset = 0; dayOffset < 60; dayOffset++)
                {
                    var slotDate = today.AddDays(dayOffset);
                    foreach (var doc in doctorProfilesList)
                    {
                        int slotCount = random.Next(1, 3);
                        for (int s = 0; s < slotCount; s++)
                        {
                            int startHour = 10 + s * 2 + random.Next(0, 2);
                            slotsList.Add(new AvailableSlot
                            {
                                DoctorProfileId = doc.Id,
                                SlotDate = slotDate,
                                SlotStart = new TimeSpan(startHour, 0, 0),
                                SlotEnd = new TimeSpan(startHour, 30, 0),
                                IsBooked = false,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
                await context.AvailableSlots.AddRangeAsync(slotsList);
                await context.SaveChangesAsync();
            }
        }
    }
}
