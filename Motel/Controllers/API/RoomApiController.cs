using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;

[Route("api/room")]
[ApiController]
public class RoomApiController : ControllerBase
{
    private readonly MotelDbContext _context;

    public RoomApiController(MotelDbContext context)
    {
        _context = context;
    }

    [HttpGet("byProperty/{propertyId}")]
    public async Task<IActionResult> GetRooms(int propertyId)
    {
        var rooms = await _context.Rooms
            .Where(r => r.PropertyId == propertyId)
            .Select(r => new
            {
                r.RoomId,
                r.RoomName
            })
            .ToListAsync();

        return Ok(rooms);
    }
}