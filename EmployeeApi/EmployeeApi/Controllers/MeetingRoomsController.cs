using EmployeeApi.Data;
using EmployeeApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApi.Controllers;

[ApiController]
[Route("api/meeting-rooms")]
public class MeetingRoomsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MeetingRoom>>> GetAll()
    {
        var rooms = await db.MeetingRooms
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MeetingRoom>> GetById(int id)
    {
        var room = await db.MeetingRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        return room is null ? NotFound() : Ok(room);
    }
}