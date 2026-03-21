using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;

[Route("api/property")]
[ApiController]
public class PropertyApiController : ControllerBase
{
    private readonly MotelDbContext _context;

    public PropertyApiController(MotelDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProperties()
    {
        var properties = await _context.Properties
            .Select(p => new
            {
                p.PropertyId,
                p.Name
            })
            .ToListAsync();

        return Ok(properties);
    }
}