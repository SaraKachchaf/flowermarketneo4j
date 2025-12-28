using Microsoft.AspNetCore.Identity;
using Neo4j.Driver;

namespace backend.Auth
{
    public class Neo4jRoleStore : IRoleStore<IdentityRole>
    {
        private readonly Neo4jService _neo4j;

        public Neo4jRoleStore(Neo4jService neo4j)
        {
            _neo4j = neo4j;
        }

        public void Dispose() { }

        public async Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            var cypher = "CREATE (r:Role {id: $id, name: $name, normalizedName: $normalizedName}) RETURN r";
            await _neo4j.RunQueryAsync(cypher, new 
            { 
                id = role.Id, 
                name = role.Name, 
                normalizedName = role.NormalizedName 
            });
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (r:Role {id: $id}) DETACH DELETE r";
            await _neo4j.RunQueryAsync(cypher, new { id = role.Id });
            return IdentityResult.Success;
        }

        public async Task<IdentityRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (r:Role {id: $id}) RETURN r";
            var result = await _neo4j.RunQueryAsync(cypher, new { id = roleId });
            return MapToRole(result.FirstOrDefault()?.GetValueOrDefault("r") as INode);
        }

        public async Task<IdentityRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            var cypher = "MATCH (r:Role {normalizedName: $name}) RETURN r";
            var result = await _neo4j.RunQueryAsync(cypher, new { name = normalizedRoleName });
            return MapToRole(result.FirstOrDefault()?.GetValueOrDefault("r") as INode);
        }

        public Task<string?> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken cancellationToken) => Task.FromResult(role.NormalizedName);
        public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken) => Task.FromResult(role.Id);
        public Task<string?> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken) => Task.FromResult(role.Name);

        public Task SetNormalizedRoleNameAsync(IdentityRole role, string? normalizedName, CancellationToken cancellationToken)
        {
            role.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetRoleNameAsync(IdentityRole role, string? roleName, CancellationToken cancellationToken)
        {
            role.Name = roleName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            var cypher = @"
                MATCH (r:Role {id: $id})
                SET r.name = $name,
                    r.normalizedName = $normalizedName
                RETURN r";
            await _neo4j.RunQueryAsync(cypher, new 
            { 
                id = role.Id, 
                name = role.Name, 
                normalizedName = role.NormalizedName 
            });
            return IdentityResult.Success;
        }

        private IdentityRole? MapToRole(INode? node)
        {
            if (node == null) return null;
            return new IdentityRole
            {
                Id = node.Properties["id"].ToString(),
                Name = node.Properties["name"].ToString(),
                NormalizedName = node.Properties["normalizedName"].ToString()
            };
        }
    }
}
