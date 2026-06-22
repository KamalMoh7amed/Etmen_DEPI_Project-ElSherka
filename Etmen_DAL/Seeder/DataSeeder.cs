
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
            // Always clean up old seed data first to ensure fresh and updated seeding
            {
                await context.AvailableSlots.ExecuteDeleteAsync();
                await context.Appointments.ExecuteDeleteAsync();
                await context.DoctorProviders.ExecuteDeleteAsync();
                await context.Reviews.ExecuteDeleteAsync();
                await context.EmergencyRequests.ExecuteDeleteAsync();
                await context.RiskAssessments.ExecuteDeleteAsync();
                await context.DoctorProfiles.ExecuteDeleteAsync();
                await context.PatientProfiles.ExecuteDeleteAsync();
                await context.HealthcareProviders.ExecuteDeleteAsync();
                await context.StaffProfiles.ExecuteDeleteAsync();

                var oldUsersQuery = context.Users.Where(u => u.Email != null && (u.Email.StartsWith("doctor") || u.Email.StartsWith("patient")) && u.Email.EndsWith("@etmen.com"));
                var oldUserIdsQuery = oldUsersQuery.Select(u => u.Id);

                await context.Notifications.Where(n => oldUserIdsQuery.Contains(n.UserId)).ExecuteDeleteAsync();
                await context.UserRoles.Where(ur => oldUserIdsQuery.Contains(ur.UserId)).ExecuteDeleteAsync();
                await oldUsersQuery.ExecuteDeleteAsync();
            }

            var governorates27 = new[]
            {
                new { Name = "القاهرة", Lat = 30.0444m, Lng = 31.2357m },
                new { Name = "الجيزة", Lat = 30.0131m, Lng = 31.2089m },
                new { Name = "الإسكندرية", Lat = 31.2001m, Lng = 29.9187m },
                new { Name = "الدقهلية", Lat = 31.0409m, Lng = 31.3785m },
                new { Name = "البحر الأحمر", Lat = 27.2579m, Lng = 33.8116m },
                new { Name = "البحيرة", Lat = 31.0364m, Lng = 30.4688m },
                new { Name = "الفيوم", Lat = 29.3084m, Lng = 30.8428m },
                new { Name = "الغربية", Lat = 30.7865m, Lng = 31.0004m },
                new { Name = "الإسماعيلية", Lat = 30.6043m, Lng = 32.2723m },
                new { Name = "المنوفية", Lat = 30.5510m, Lng = 31.0120m },
                new { Name = "المنيا", Lat = 28.0871m, Lng = 30.7618m },
                new { Name = "القليوبية", Lat = 30.4591m, Lng = 31.1856m },
                new { Name = "الوادي الجديد", Lat = 25.4390m, Lng = 30.5486m },
                new { Name = "السويس", Lat = 29.9668m, Lng = 32.5498m },
                new { Name = "أسوان", Lat = 24.0889m, Lng = 32.8998m },
                new { Name = "أسيوط", Lat = 27.1783m, Lng = 31.1859m },
                new { Name = "بني سويف", Lat = 29.0744m, Lng = 31.0979m },
                new { Name = "بورسعيد", Lat = 31.2653m, Lng = 32.3019m },
                new { Name = "دمياط", Lat = 31.4175m, Lng = 31.8144m },
                new { Name = "الشرقية", Lat = 30.5877m, Lng = 31.5020m },
                new { Name = "جنوب سيناء", Lat = 28.2403m, Lng = 33.6231m },
                new { Name = "كفر الشيخ", Lat = 31.1107m, Lng = 30.9388m },
                new { Name = "مطروح", Lat = 31.3543m, Lng = 27.2373m },
                new { Name = "قنا", Lat = 26.1551m, Lng = 32.7160m },
                new { Name = "شمال سيناء", Lat = 31.1321m, Lng = 33.8032m },
                new { Name = "سوهاج", Lat = 26.5591m, Lng = 31.6948m },
                new { Name = "الأقصر", Lat = 25.6872m, Lng = 32.6396m }
            };

            var random = new Random(42);
            var providers = new List<HealthcareProvider>();

            // Seed 100 Hospitals distributed across 27 governorates
            for (int i = 1; i <= 100; i++)
            {
                var gov = governorates27[(i - 1) % governorates27.Length];
                var offsetLat = (decimal)(random.NextDouble() - 0.5) * 0.15m;
                var offsetLng = (decimal)(random.NextDouble() - 0.5) * 0.15m;
                providers.Add(new HealthcareProvider
                {
                    Name = $"مستشفى طوارئ {GetHospitalName(i)}",
                    Type = "Hospital",
                    Latitude = gov.Lat + offsetLat,
                    Longitude = gov.Lng + offsetLng,
                    Address = $"{gov.Name} - وسط المدينة",
                    Phone = $"010{random.Next(10000000, 99999999)}",
                    AvailableBeds = random.Next(10, 150),
                    IsEmergencyCenter = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Seed 200 Clinics distributed across 27 governorates
            for (int i = 1; i <= 200; i++)
            {
                var gov = governorates27[(i - 1) % governorates27.Length];
                var offsetLat = (decimal)(random.NextDouble() - 0.5) * 0.15m;
                var offsetLng = (decimal)(random.NextDouble() - 0.5) * 0.15m;
                providers.Add(new HealthcareProvider
                {
                    Name = $"عيادة {GetClinicName(i)} الطبية",
                    Type = "Clinic",
                    Latitude = gov.Lat + offsetLat,
                    Longitude = gov.Lng + offsetLng,
                    Address = $"{gov.Name} - الحي السكني",
                    Phone = $"011{random.Next(10000000, 99999999)}",
                    AvailableBeds = 0,
                    IsEmergencyCenter = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Seed 150 Centers distributed across 27 governorates
            for (int i = 1; i <= 150; i++)
            {
                var gov = governorates27[(i - 1) % governorates27.Length];
                var offsetLat = (decimal)(random.NextDouble() - 0.5) * 0.15m;
                var offsetLng = (decimal)(random.NextDouble() - 0.5) * 0.15m;
                providers.Add(new HealthcareProvider
                {
                    Name = $"مركز {GetCenterName(i)} الطبي",
                    Type = "Center",
                    Latitude = gov.Lat + offsetLat,
                    Longitude = gov.Lng + offsetLng,
                    Address = $"{gov.Name} - المجمع الطبي",
                    Phone = $"012{random.Next(10000000, 99999999)}",
                    AvailableBeds = random.Next(5, 50),
                    IsEmergencyCenter = random.Next(2) == 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.HealthcareProviders.AddRangeAsync(providers);
            await context.SaveChangesAsync();

            // Seed 550 Doctors
            var doctorUsers = new List<ApplicationUser>();
            var doctorProfiles = new List<DoctorProfile>();
            var doctorRoles = new List<IdentityUserRole<string>>();

            var doctorRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Doctor");
            var doctorRoleId = doctorRole?.Id ?? Guid.NewGuid().ToString();

            var specializations = new[] { "قلب وأوعية دموية", "أطفال", "أسنان", "عظام", "جلدية", "مخ وأعصاب", "رمد", "باطنة", "جراحة عامة", "نساء وتوليد" };
            var docFirstNames = new[] { "محمد", "أحمد", "محمود", "علي", "عمر", "خالد", "يوسف", "إبراهيم", "طارق", "حسام" };
            var docLastNames = new[] { "الشرقاوي", "سالم", "عبد العزيز", "عوض", "المنشاوي", "الحداد", "حسني", "سليمان", "شاكر", "سعيد" };

            var doctorAvatarUrls = new[]
            {
                "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&q=80&w=150", // male doc 1
                "https://images.unsplash.com/photo-1594824813573-246434de83fb?auto=format&fit=crop&q=80&w=150", // female doc 2
                "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&q=80&w=150", // male doc 3
                "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&q=80&w=150", // female doc 4
                "https://images.unsplash.com/photo-1537368910025-700350fe46c7?auto=format&fit=crop&q=80&w=150", // male doc 5
                "https://images.unsplash.com/photo-1614608682850-e0d6ed316d47?auto=format&fit=crop&q=80&w=150", // female doc 6
                "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?auto=format&fit=crop&q=80&w=150", // female doc 7
                "https://images.unsplash.com/photo-1582973512617-e56f83bec2c4?auto=format&fit=crop&q=80&w=150", // male doc 8
                "https://images.unsplash.com/photo-1607990283143-e81e7a2c93ab?auto=format&fit=crop&q=80&w=150", // male doc 9
                "https://images.unsplash.com/photo-1623574788204-7b401c327c90?auto=format&fit=crop&q=80&w=150"  // female doc 10
            };

            var passwordHasher = new PasswordHasher<ApplicationUser>();
            string docHashedPassword = passwordHasher.HashPassword(null!, "Doc@123");

            var hospitals = providers.Where(p => p.Type == "Hospital").ToList();
            var clinics = providers.Where(p => p.Type == "Clinic").ToList();
            var centers = providers.Where(p => p.Type == "Center").ToList();

            for (int i = 1; i <= 550; i++)
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
                    ProfilePicture = doctorAvatarUrls[(i - 1) % doctorAvatarUrls.Length],
                    PasswordHash = docHashedPassword,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                doctorUsers.Add(user);
                doctorRoles.Add(new IdentityUserRole<string> { UserId = docId, RoleId = doctorRoleId });

                string specialization = specializations[random.Next(specializations.Length)];
                var profile = new DoctorProfile
                {
                    ApplicationUserId = docId,
                    FullName = $"د. {firstName} {lastName}",
                    Specialization = specialization,
                    LicenseNumber = $"LIC{random.Next(100000, 999999)}",
                    YearsOfExperience = random.Next(3, 25),
                    ConsultationFee = random.Next(100, 500),
                    IsAvailable = true,
                    IsOnboarded = true,
                    CreatedAt = DateTime.UtcNow
                };
                doctorProfiles.Add(profile);
            }

            await context.Users.AddRangeAsync(doctorUsers);
            await context.UserRoles.AddRangeAsync(doctorRoles);
            await context.DoctorProfiles.AddRangeAsync(doctorProfiles);
            await context.SaveChangesAsync();

            // Link Doctors to Healthcare Providers via DoctorProvider and populate OnboardingDataJson
            var doctorProviders = new List<DoctorProvider>();
            for (int i = 0; i < 200; i++)
            {
                var doctorProfile = doctorProfiles[i];
                var hospital = hospitals[i / 2]; // 2 doctors per hospital (100 hospitals total)
                doctorProviders.Add(new DoctorProvider
                {
                    DoctorProfileId = doctorProfile.Id,
                    HealthcareProviderId = hospital.Id,
                    IsEmergencyDoctor = (i % 2 == 0),
                    IsOwner = false,
                    AffiliationRole = i % 2 == 0 ? "ER Consultant" : "Specialist"
                });

                doctorProfile.OnboardingDataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    EntityName = hospital.Name,
                    EntityType = hospital.Type,
                    BranchArabicName = hospital.Name,
                    City = hospital.Address?.Split('-').FirstOrDefault()?.Trim() ?? "القاهرة",
                    Area = hospital.Address?.Split('-').LastOrDefault()?.Trim() ?? "وسط المدينة",
                    BranchMobile = hospital.Phone,
                    Latitude = hospital.Latitude,
                    Longitude = hospital.Longitude,
                    HealthcareProviderId = hospital.Id
                });
            }

            for (int i = 200; i < 400; i++)
            {
                var doctorProfile = doctorProfiles[i];
                var clinic = clinics[i - 200]; // 1 doctor per clinic (200 clinics total)
                doctorProviders.Add(new DoctorProvider
                {
                    DoctorProfileId = doctorProfile.Id,
                    HealthcareProviderId = clinic.Id,
                    IsEmergencyDoctor = false,
                    IsOwner = true,
                    AffiliationRole = "Owner"
                });

                doctorProfile.OnboardingDataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    EntityName = clinic.Name,
                    EntityType = clinic.Type,
                    BranchArabicName = clinic.Name,
                    City = clinic.Address?.Split('-').FirstOrDefault()?.Trim() ?? "القاهرة",
                    Area = clinic.Address?.Split('-').LastOrDefault()?.Trim() ?? "الحي السكني",
                    BranchMobile = clinic.Phone,
                    Latitude = clinic.Latitude,
                    Longitude = clinic.Longitude,
                    HealthcareProviderId = clinic.Id
                });
            }

            for (int i = 400; i < 550; i++)
            {
                var doctorProfile = doctorProfiles[i];
                var center = centers[i - 400]; // 1 doctor per center (150 centers total)
                doctorProviders.Add(new DoctorProvider
                {
                    DoctorProfileId = doctorProfile.Id,
                    HealthcareProviderId = center.Id,
                    IsEmergencyDoctor = center.IsEmergencyCenter,
                    IsOwner = false,
                    AffiliationRole = "Staff Doctor"
                });

                doctorProfile.OnboardingDataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    EntityName = center.Name,
                    EntityType = center.Type,
                    BranchArabicName = center.Name,
                    City = center.Address?.Split('-').FirstOrDefault()?.Trim() ?? "القاهرة",
                    Area = center.Address?.Split('-').LastOrDefault()?.Trim() ?? "المجمع الطبي",
                    BranchMobile = center.Phone,
                    Latitude = center.Latitude,
                    Longitude = center.Longitude,
                    HealthcareProviderId = center.Id
                });
            }

            context.DoctorProfiles.UpdateRange(doctorProfiles);
            await context.DoctorProviders.AddRangeAsync(doctorProviders);
            await context.SaveChangesAsync();

            // Seed 2000 Patients distributed across all 27 Governorates in Egypt
            var patientUsers = new List<ApplicationUser>();
            var patientProfiles = new List<PatientProfile>();
            var patientRoles = new List<IdentityUserRole<string>>();

            var patientRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Patient");
            var patientRoleId = patientRole?.Id ?? Guid.NewGuid().ToString();

            var patFirstNames = new[] { "أحمد", "سامي", "رائد", "هشام", "كريم", "خالد", "عبد الله", "حامد", "نادر", "مصطفى" };
            var patLastNames = new[] { "سالم", "جلال", "حسن", "منصور", "شاهين", "رزق", "الخولي", "البغدادي", "حسين", "فوزي" };

            string patHashedPassword = passwordHasher.HashPassword(null!, "Pat@123");

            for (int i = 1; i <= 2000; i++)
            {
                var patId = Guid.NewGuid().ToString();
                var email = $"patient{i}@etmen.com";
                var firstName = patFirstNames[random.Next(patFirstNames.Length)];
                var lastName = patLastNames[random.Next(patLastNames.Length)];

                // Distribute evenly across 27 governorates
                var gov = governorates27[(i - 1) % governorates27.Length];
                var offsetLat = (decimal)(random.NextDouble() - 0.5) * 0.15m;
                var offsetLng = (decimal)(random.NextDouble() - 0.5) * 0.15m;

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
                    Latitude = gov.Lat + offsetLat,
                    Longitude = gov.Lng + offsetLng,
                    CreatedAt = DateTime.UtcNow
                };
                patientProfiles.Add(profile);
            }

            await context.Users.AddRangeAsync(patientUsers);
            await context.UserRoles.AddRangeAsync(patientRoles);
            await context.PatientProfiles.AddRangeAsync(patientProfiles);
            await context.SaveChangesAsync();

            // Sanity check to make sure IDs were generated by database save
            if (doctorProfiles.Any(d => d.Id == 0))
            {
                throw new InvalidOperationException("One or more DoctorProfiles have an Id of 0 before seeding slots!");
            }
            if (patientProfiles.Any(p => p.Id == 0))
            {
                throw new InvalidOperationException("One or more PatientProfiles have an Id of 0 before seeding slots!");
            }

            // Seed Available Slots & Booked Appointments from -7 days to +7 days
            var availableSlots = new List<AvailableSlot>();
            var appointmentsList = new List<Appointment>();
            var today = DateTime.Today;

            context.ChangeTracker.AutoDetectChangesEnabled = false;

            for (int dayOffset = -7; dayOffset <= 7; dayOffset++)
            {
                var date = today.AddDays(dayOffset);
                foreach (var doctorProfile in doctorProfiles)
                {
                    var slotTimes = new[]
                    {
                        new { Start = new TimeSpan(10, 0, 0), End = new TimeSpan(10, 30, 0) },
                        new { Start = new TimeSpan(11, 0, 0), End = new TimeSpan(11, 30, 0) },
                        new { Start = new TimeSpan(14, 0, 0), End = new TimeSpan(14, 30, 0) },
                        new { Start = new TimeSpan(15, 0, 0), End = new TimeSpan(15, 30, 0) }
                    };

                    foreach (var time in slotTimes)
                    {
                        var isBooked = random.Next(3) == 0; // 33% chance to be booked
                        var slot = new AvailableSlot
                        {
                            DoctorProfileId = doctorProfile.Id,
                            SlotDate = date,
                            SlotStart = time.Start,
                            SlotEnd = time.End,
                            IsBooked = isBooked,
                            CreatedAt = DateTime.UtcNow.AddDays(-10)
                        };
                        availableSlots.Add(slot);

                        if (isBooked)
                        {
                            var patientProfile = patientProfiles[random.Next(patientProfiles.Count)];
                            AppointmentStatus status = AppointmentStatus.Scheduled;
                            if (dayOffset < 0)
                            {
                                status = random.Next(5) == 0 ? AppointmentStatus.NoShow : AppointmentStatus.Completed;
                            }
                            else
                            {
                                status = random.Next(5) == 0 ? AppointmentStatus.Cancelled : (random.Next(2) == 0 ? AppointmentStatus.Confirmed : AppointmentStatus.Scheduled);
                            }

                            var appointment = new Appointment
                            {
                                PatientProfileId = patientProfile.Id,
                                DoctorProfileId = doctorProfile.Id,
                                AppointmentDate = date,
                                StartTime = time.Start,
                                EndTime = time.End,
                                Status = status,
                                Notes = random.Next(2) == 0 ? "متابعة طبية دورية للتحقق من الحالة العامة للفحوصات." : null,
                                CreatedAt = DateTime.UtcNow.AddDays(-8)
                            };
                            appointmentsList.Add(appointment);
                        }
                    }
                }
            }

            await context.AvailableSlots.AddRangeAsync(availableSlots);
            await context.Appointments.AddRangeAsync(appointmentsList);

            context.ChangeTracker.AutoDetectChangesEnabled = true;
            await context.SaveChangesAsync();

            // Seed cases between moderate (Medium) and critical (Critical) for the 2000 Patients
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

            var riskLevels = new[] { RiskLevel.Medium, RiskLevel.High, RiskLevel.Critical };

            for (int i = 0; i < 2000; i++)
            {
                var patientProfile = patientProfiles[i];
                var level = riskLevels[random.Next(riskLevels.Length)];
                decimal riskScore = 0m;
                string symptoms = "";
                string recommendations = "";
                bool isEmergency = false;

                if (level == RiskLevel.Medium)
                {
                    riskScore = 0.35m + (decimal)(random.NextDouble() * 0.15); // 0.35 to 0.50
                    symptoms = "Fever,Cough,Fatigue";
                    recommendations = "[\"ارتاح بالمنزل\",\"تناول سوائل دافئة\",\"راقب درجة حرارتك\"]";
                    isEmergency = false;
                }
                else if (level == RiskLevel.High)
                {
                    riskScore = 0.55m + (decimal)(random.NextDouble() * 0.15); // 0.55 to 0.70
                    symptoms = "HighFever,Cough,Dyspnea,Fatigue";
                    recommendations = "[\"استشر طبيباً فوراً\",\"اعزل نفسك\",\"راقب مستوى الأكسجين\"]";
                    isEmergency = false;
                }
                else // Critical
                {
                    riskScore = 0.75m + (decimal)(random.NextDouble() * 0.20); // 0.75 to 0.95
                    symptoms = "ChestPain,Dyspnea,LossOfConsciousness";
                    recommendations = "[\"اتصل بالإسعاف فوراً\",\"استلقِ على ظهرك\",\"لا تبذل مجهوداً\"]";
                    isEmergency = true;
                }

                var risk = new RiskAssessment
                {
                    PatientProfileId = patientProfile.Id,
                    AssessmentDate = DateTime.UtcNow.AddMinutes(-random.Next(10, 120)),
                    RiskScore = riskScore,
                    RiskLevel = level,
                    Symptoms = symptoms,
                    RecommendationsJson = recommendations,
                    IsEmergency = isEmergency,
                    CreatedAt = DateTime.UtcNow
                };
                riskAssessments.Add(risk);
            }

            await context.RiskAssessments.AddRangeAsync(riskAssessments);
            await context.SaveChangesAsync();

            // Seed EmergencyRequests for Critical risk patients (~660 requests expected)
            // Distribute statuses: 20% Pending, 20% Accepted, 60% Completed
            var activeProviders = providers.Where(p => p.IsEmergencyCenter || p.Type == "Hospital").ToList();

            for (int i = 0; i < 2000; i++)
            {
                var risk = riskAssessments[i];
                if (risk.RiskLevel == RiskLevel.Critical)
                {
                    var patientProfile = patientProfiles[i];
                    var reqIdx = random.Next(emergencyTypes.Length);

                    // Determine status distribution
                    var roll = random.Next(100);
                    EmergencyRequestStatus status = EmergencyRequestStatus.Pending;
                    if (roll >= 20 && roll < 40)
                    {
                        status = EmergencyRequestStatus.Accepted;
                    }
                    else if (roll >= 40)
                    {
                        status = EmergencyRequestStatus.Completed;
                    }

                    var request = new EmergencyRequest
                    {
                        PatientProfileId = patientProfile.Id,
                        RiskAssessmentId = risk.Id,
                        Status = status,
                        EmergencyType = emergencyTypes[reqIdx],
                        Description = emergencyDescriptions[reqIdx],
                        Latitude = patientProfile.Latitude,
                        Longitude = patientProfile.Longitude,
                        PriorityScore = random.Next(80, 100),
                        RequestedAt = DateTime.UtcNow.AddMinutes(-random.Next(5, 60)),
                        IsAutoGenerated = true
                    };

                    if ((status == EmergencyRequestStatus.Accepted || status == EmergencyRequestStatus.Completed) && activeProviders.Any())
                    {
                        var assignedProvider = activeProviders[random.Next(activeProviders.Count)];
                        request.HealthcareProviderId = assignedProvider.Id;
                        request.AcceptedAt = request.RequestedAt.AddMinutes(random.Next(2, 10));

                        if (status == EmergencyRequestStatus.Completed)
                        {
                            request.CompletedAt = request.AcceptedAt.Value.AddMinutes(random.Next(15, 45));
                            request.ResponseNotes = "تم تقديم الرعاية الطبية اللازمة ونقل المريض بنجاح.";
                        }
                    }

                    emergencyRequests.Add(request);
                }
            }

            await context.EmergencyRequests.AddRangeAsync(emergencyRequests);
            await context.SaveChangesAsync();

            // Seed Doctor Reviews and Ratings
            var reviews = new List<Review>();
            var comments = new[]
            {
                "دكتور ممتاز وخلوق جداً، شرح لي حالتي بالتفصيل ووصف العلاج المناسب.",
                "طبيب محترف ومستمع جيد للمريض. أنصح بالتعامل معه.",
                "العيادة نظيفة والخدمة ممتازة، الدكتور متعاون جداً.",
                "تشخيص دقيق ومعاملة راقية جداً من الدكتور وطاقم التمريض.",
                "طبيب ممتاز ذو خبرة عالية، جزاه الله خيراً.",
                "كانت تجربة مريحة جداً، الدكتور بسط لي الأمور وتطمنت كثير.",
                "أنصح به بشدة، دكتور متمكن ومخلص في عمله.",
                "أفضل دكتور تعاملت معه، تشخيصه رائع وسريع.",
                "طبيب متميز جداً يهتم بأدق التفاصيل وصحة المريض.",
                "معاملة ممتازة ووقت انتظار قصير جداً داخل العيادة."
            };

            foreach (var doctorProfile in doctorProfiles)
            {
                // Seed 2-3 reviews per doctor
                int numReviews = random.Next(2, 4);
                for (int rIdx = 0; rIdx < numReviews; rIdx++)
                {
                    var patientProfile = patientProfiles[random.Next(patientProfiles.Count)];
                    var rating = random.Next(4, 6); // 4 or 5 stars
                    var comment = comments[random.Next(comments.Length)];
                    
                    reviews.Add(new Review
                    {
                        DoctorProfileId = doctorProfile.Id,
                        PatientProfileId = patientProfile.Id,
                        Rating = rating,
                        Comment = comment,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                    });
                }
            }

            await context.Reviews.AddRangeAsync(reviews);
            await context.SaveChangesAsync();

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

            // 6️⃣ Link staff@etmen.com to the first Hospital if not already linked
            var existingStaffUser = await userManager.FindByEmailAsync("staff@etmen.com");
            if (existingStaffUser != null)
            {
                var hasProfile = await context.StaffProfiles.AnyAsync(sp => sp.ApplicationUserId == existingStaffUser.Id);
                if (!hasProfile)
                {
                    var firstHospital = await context.HealthcareProviders.FirstOrDefaultAsync(p => p.Type == "Hospital");
                    if (firstHospital != null)
                    {
                        var staffProfile = new StaffProfile
                        {
                            ApplicationUserId = existingStaffUser.Id,
                            HealthcareProviderId = firstHospital.Id,
                            RoleType = StaffRoleType.Receptionist,
                            ActiveShift = StaffShiftType.Morning,
                            IsInvitationAccepted = true,
                            JoinedAt = DateTime.UtcNow
                        };
                        context.StaffProfiles.Add(staffProfile);
                        await context.SaveChangesAsync();
                    }
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

        private static string GetCenterName(int i)
        {
            var names = new[] { "التميز", "الشفاء السريع", "الخدمة المتميزة", "الرعاية المتكاملة", "النخبة التخصصي", "الهدى الطبي", "الحرمين", "الأمل والشفاء", "النجمة", "القدس" };
            return names[(i - 1) % names.Length] + " " + ((i - 1) / names.Length + 1);
        }
    }
}
