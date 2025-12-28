using backend.Models;
using Microsoft.AspNetCore.Identity;
using Neo4j.Driver;
using System.Security.Claims;

namespace backend.Auth
{
    public class Neo4jUserStore : 
        IUserStore<AppUser>, 
        IUserPasswordStore<AppUser>, 
        IUserEmailStore<AppUser>,
        IUserRoleStore<AppUser>
    {
        private readonly Neo4jService _neo4j;

        public Neo4jUserStore(Neo4jService neo4j)
        {
            _neo4j = neo4j;
        }

        public void Dispose() { }

        // --- IUserStore ---

        public async Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cypher = @"
                CREATE (u:User {
                    id: $id,
                    userName: $userName,
                    normalizedUserName: $normalizedUserName,
                    email: $email,
                    normalizedEmail: $normalizedEmail,
                    passwordHash: $passwordHash,
                    fullName: $fullName,
                    isApproved: $isApproved,
                    createdAt: $createdAt,
                    emailConfirmed: $emailConfirmed,
                    emailVerificationCode: $emailVerificationCode,
                    emailVerificationCodeExpiry: $emailVerificationCodeExpiry
                })
                RETURN u";

            var parameters = new Dictionary<string, object>
            {
                { "id", user.Id },
                { "userName", user.UserName },
                { "normalizedUserName", user.NormalizedUserName },
                { "email", user.Email },
                { "normalizedEmail", user.NormalizedEmail },
                { "passwordHash", user.PasswordHash },
                { "fullName", user.FullName },
                { "isApproved", user.IsApproved },
                { "createdAt", user.CreatedAt.ToString("O") },
                { "emailConfirmed", user.EmailConfirmed },
                { "emailVerificationCode", user.EmailVerificationCode },
                { "emailVerificationCodeExpiry", user.EmailVerificationCodeExpiry?.ToString("O") }
            };

            await _neo4j.RunQueryAsync(cypher, parameters);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (u:User {id: $id}) DETACH DELETE u";
            await _neo4j.RunQueryAsync(cypher, new { id = user.Id });
            return IdentityResult.Success;
        }

        public async Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (u:User {id: $id}) RETURN u";
            var result = await _neo4j.RunQueryAsync(cypher, new { id = userId });
            return MapToUser(result.FirstOrDefault()?.GetValueOrDefault("u") as INode);
        }

        public async Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (u:User {normalizedUserName: $name}) RETURN u";
            var result = await _neo4j.RunQueryAsync(cypher, new { name = normalizedUserName });
            return MapToUser(result.FirstOrDefault()?.GetValueOrDefault("u") as INode);
        }

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);

        public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken)
        {
            var cypher = @"
                MATCH (u:User {id: $id})
                SET u.userName = $userName,
                    u.normalizedUserName = $normalizedUserName,
                    u.email = $email,
                    u.normalizedEmail = $normalizedEmail,
                    u.passwordHash = $passwordHash,
                    u.fullName = $fullName,
                    u.isApproved = $isApproved,
                    u.emailConfirmed = $emailConfirmed,
                    u.emailVerificationCode = $emailVerificationCode,
                    u.emailVerificationCodeExpiry = $emailVerificationCodeExpiry
                RETURN u";

            var parameters = new Dictionary<string, object>
            {
                { "id", user.Id },
                { "userName", user.UserName },
                { "normalizedUserName", user.NormalizedUserName },
                { "email", user.Email },
                { "normalizedEmail", user.NormalizedEmail },
                { "passwordHash", user.PasswordHash },
                { "fullName", user.FullName },
                { "isApproved", user.IsApproved },
                { "emailConfirmed", user.EmailConfirmed },
                { "emailVerificationCode", user.EmailVerificationCode },
                { "emailVerificationCodeExpiry", user.EmailVerificationCodeExpiry?.ToString("O") }
            };

            await _neo4j.RunQueryAsync(cypher, parameters);
            return IdentityResult.Success;
        }

        // --- IUserPasswordStore ---

        public Task<string?> GetPasswordHashAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);
        public Task<bool> HasPasswordAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        public Task SetPasswordHashAsync(AppUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        // --- IUserEmailStore ---

        public async Task<AppUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (u:User {normalizedEmail: $email}) RETURN u";
            var result = await _neo4j.RunQueryAsync(cypher, new { email = normalizedEmail });
            return MapToUser(result.FirstOrDefault()?.GetValueOrDefault("u") as INode);
        }

        public Task<string?> GetEmailAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email);
        public Task<bool> GetEmailConfirmedAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);
        public Task<string?> GetNormalizedEmailAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmail);

        public Task SetEmailAsync(AppUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task SetEmailConfirmedAsync(AppUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task SetNormalizedEmailAsync(AppUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        // --- IUserRoleStore ---

        public async Task AddToRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            var cypher = @"
                MATCH (u:User {id: $id})
                MATCH (r:Role {normalizedName: $roleName})
                MERGE (u)-[:HAS_ROLE]->(r)";
            await _neo4j.RunQueryAsync(cypher, new { id = user.Id, roleName = roleName.ToUpper() });
        }

        public async Task<IList<string>> GetRolesAsync(AppUser user, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (u:User {id: $id})-[:HAS_ROLE]->(r:Role) RETURN r.name as name";
            var result = await _neo4j.RunQueryAsync(cypher, new { id = user.Id });
            return result.Select(r => r["name"].ToString()).ToList();
        }

        public async Task<IList<AppUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (u:User)-[:HAS_ROLE]->(r:Role {normalizedName: $roleName}) RETURN u";
            var result = await _neo4j.RunQueryAsync(cypher, new { roleName = roleName.ToUpper() });
            return result.Select(r => MapToUser(r["u"] as INode)).ToList();
        }

        public async Task<bool> IsInRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (u:User {id: $id})-[:HAS_ROLE]->(r:Role {normalizedName: $roleName}) RETURN count(r) > 0 as result";
            var result = await _neo4j.RunQueryAsync(cypher, new { id = user.Id, roleName = roleName.ToUpper() });
            return (bool)result.FirstOrDefault()?["result"];
        }

        public async Task RemoveFromRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (u:User {id: $id})-[rel:HAS_ROLE]->(r:Role {normalizedName: $roleName}) DELETE rel";
            await _neo4j.RunQueryAsync(cypher, new { id = user.Id, roleName = roleName.ToUpper() });
        }

        // --- Mapper ---

        private AppUser? MapToUser(INode? node)
        {
            if (node == null) return null;

            var user = new AppUser
            {
                Id = node.Properties["id"].ToString(),
                UserName = node.Properties.GetValueOrDefault("userName")?.ToString(),
                NormalizedUserName = node.Properties.GetValueOrDefault("normalizedUserName")?.ToString(),
                Email = node.Properties.GetValueOrDefault("email")?.ToString(),
                NormalizedEmail = node.Properties.GetValueOrDefault("normalizedEmail")?.ToString(),
                PasswordHash = node.Properties.GetValueOrDefault("passwordHash")?.ToString(),
                FullName = node.Properties.GetValueOrDefault("fullName")?.ToString() ?? "",
                IsApproved = (bool)(node.Properties.GetValueOrDefault("isApproved") ?? false),
                EmailConfirmed = (bool)(node.Properties.GetValueOrDefault("emailConfirmed") ?? false),
                EmailVerificationCode = node.Properties.GetValueOrDefault("emailVerificationCode")?.ToString(),
                CreatedAt = DateTime.Parse(node.Properties["createdAt"].ToString())
            };

            if (node.Properties.TryGetValue("emailVerificationCodeExpiry", out var expiry) && expiry != null && expiry.ToString() != "")
            {
                user.EmailVerificationCodeExpiry = DateTime.Parse(expiry.ToString());
            }

            return user;
        }
    }
}
