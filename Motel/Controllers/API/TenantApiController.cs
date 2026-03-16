using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;

[Route("api/tenant")]
[ApiController]
public class TenantApiController : ControllerBase
{
    private readonly MotelDbContext _context;

    public TenantApiController(MotelDbContext context)
    {
        _context = context;
    }

    [HttpGet("byRoom/{roomId}")]
    public async Task<IActionResult> GetTenants(int roomId)
    {
        var tenants = await _context.RoomOccupancies
            .Where(ro => ro.RoomId == roomId && ro.MoveOutDate == null)
            .Select(ro => new
            {
                ro.Tenant.TenantId,
                ro.Tenant.FullName
            })
            .ToListAsync();

        return Ok(tenants);
    }
}