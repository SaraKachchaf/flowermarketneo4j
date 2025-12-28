using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Data
{
    public static class SeedData
    {
        public static async Task Initialize(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            // Définir les rôles à créer
            string[] roles = { "Admin", "Client", "Prestataire" };

            // Créer les rôles si ils n'existent pas déjà
            foreach (var role in roles)
            {
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


                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
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