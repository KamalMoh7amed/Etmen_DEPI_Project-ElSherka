using Etmen_DAL.DbContext;
using Etmen_DAL.Seed;
using Etmen_PL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register application services via DependencyInjection extension method
builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

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
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

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

// ── SignalR Hubs ───────────────────────────────────────────────
app.MapHub<Etmen_PL.Hubs.ChatHub>("/hubs/chat");
app.MapHub<Etmen_PL.Hubs.QueueHub>("/hubs/queue");
app.MapHub<Etmen_PL.Hubs.EmergencyHub>("/hubs/emergency");

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
