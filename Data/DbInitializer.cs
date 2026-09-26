using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamenParcial_Incidencias_Render.Data;

public static class DbInitializer
{
    public const string RolSupervisor = "Supervisor";
    public const string EmailSupervisor = "supervisor@test.com";
    public const string PasswordSupervisor = "Supervisor123!";

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var context = provider.GetRequiredService<ApplicationDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        if (!await roleManager.RoleExistsAsync(RolSupervisor))
        {
            await roleManager.CreateAsync(new IdentityRole(RolSupervisor));
        }

        var supervisor = await userManager.FindByEmailAsync(EmailSupervisor);
        if (supervisor is null)
        {
            supervisor = new ApplicationUser
            {
                UserName = EmailSupervisor,
                Email = EmailSupervisor,
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(supervisor, PasswordSupervisor);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo crear el usuario de prueba: " +
                    string.Join("; ", created.Errors.Select(error => error.Description)));
            }

            await userManager.AddToRoleAsync(supervisor, RolSupervisor);
        }

        if (!await context.Incidencias.AnyAsync())
        {
            await context.Incidencias.AddRangeAsync(DatosPrueba.Crear());
            await context.SaveChangesAsync();
        }
    }
}
