using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Data
{
    public static class SeedData
    {
        public static async Task Initialize(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            Console.WriteLine("SEEDING: Initializing SeedData...");
            // Définir les rôles à créer
            string[] roles = { "Admin", "Client", "Prestataire" };

            // Créer les rôles si ils n'existent pas déjà
            foreach (var role in roles)
            {
                Console.WriteLine($"SEEDING: Checking role {role}...");
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ADMIN PAR DÉFAUT
            var adminEmail = "admin@flowermarket.com";

            // Vérifier si l'admin existe déjà
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                Console.WriteLine("SEEDING: Admin user not found. Creating...");
                // Créer un nouvel admin
                var admin = new AppUser
                {
                    FullName = "Super Admin",
                    Email = adminEmail,
                    UserName = adminEmail,

                    // 🔴 AJOUTS CRITIQUES
                    EmailConfirmed = true,
                    IsApproved = true
                };


                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    Console.WriteLine("SEEDING: Admin user created successfully.");
                    await userManager.AddToRoleAsync(admin, "Admin");
                    Console.WriteLine("SEEDING: Admin assigned to Admin role.");
                }
                else
                {
                    Console.WriteLine("SEEDING ERROR: Failed to create admin: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Console.WriteLine("SEEDING: Admin user already exists. Updating properties...");
                // 🔧 FIX ADMIN EXISTANT
                adminUser.EmailConfirmed = true;
                adminUser.IsApproved = true;

                await userManager.UpdateAsync(adminUser);

                var userRoles = await userManager.GetRolesAsync(adminUser);
                if (!userRoles.Contains("Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

        }
    }
}