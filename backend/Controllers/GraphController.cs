using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/graph")]
public class GraphController : ControllerBase
{
    private readonly Neo4jService _neo4j;

    public GraphController(Neo4jService neo4j)
    {
        _neo4j = neo4j;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrdersGraph()
    {
        var cypher = @"
        MATCH (u:User)-[:PLACED]->(o:Order)-[:CONTAINS]->(p:Product)
        OPTIONAL MATCH (s:Store)-[:RECEIVED]->(o)
        RETURN u, o, p, s
        ";

        var data = await _neo4j.RunQueryAsync(cypher);
        return Ok(data);
    }
}
