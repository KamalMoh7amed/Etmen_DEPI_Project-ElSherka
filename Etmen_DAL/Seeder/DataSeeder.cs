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
        }
    }
}