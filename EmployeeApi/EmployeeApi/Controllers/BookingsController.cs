using EmployeeApi.Data;
using EmployeeApi.Models;
using EmployeeApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApi.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAll(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? roomId,
        [FromQuery] BookingStatus? status)
    {
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Participants)
            .AsQueryable();

        if (from is not null) query = query.Where(b => b.Date >= from.Value);
        if (to is not null) query = query.Where(b => b.Date <= to.Value);
        if (roomId is not null) query = query.Where(b => b.RoomId == roomId.Value);
        if (status is not null) query = query.Where(b => b.Status == status.Value);

        var bookings = await query
            .OrderBy(b => b.Date)
            .ThenBy(b => b.StartTime)
            .ToListAsync();

        return Ok(bookings.Select(BookingDto.From));
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetForEmployee(
        int employeeId,
        [FromQuery] DateOnly? from)
    {
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Participants)
            .Where(b => b.OrganizerId == employeeId
                     || b.Participants.Any(p => p.EmployeeId == employeeId));

        if (from is not null) query = query.Where(b => b.Date >= from.Value);

        var bookings = await query
            .OrderBy(b => b.Date)
            .ThenBy(b => b.StartTime)
            .ToListAsync();

        return Ok(bookings.Select(BookingDto.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDto>> GetById(int id)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == id);

        return booking is null ? NotFound() : Ok(BookingDto.From(booking));
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest request)
    {
        var room = await db.MeetingRooms.FindAsync(request.RoomId);
        if (room is null)
            return NotFound(new { message = "ოთახი ვერ მოიძებნა" });

        if (request.EndTime <= request.StartTime)
            return BadRequest(new { message = "დასრულების დრო უნდა აღემატებოდეს დაწყების დროს" });

        if (request.Date.ToDateTime(request.StartTime) < DateTime.Now.AddMinutes(-1))
            return BadRequest(new { message = "წარსულ დროზე დაჯავშნა შეუძლებელია" });

        var participantIds = request.ParticipantIds
            .Where(id => id != request.OrganizerId)
            .Distinct()
            .ToList();

        if (participantIds.Count + 1 > room.Capacity)
            return BadRequest(new { message = $"ოთახის ტევადობაა {room.Capacity} ადამიანი" });

        if (await HasConflictAsync(room.Id, request.Date, request.StartTime, request.EndTime))
            return Conflict(new { message = "ოთახი ამ დროს უკვე დაკავებულია" });

        var booking = new Booking
        {
            RoomId = room.Id,
            Title = request.Title.Trim(),
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            OrganizerId = request.OrganizerId,
            Status = room.RequiresApproval ? BookingStatus.Pending : BookingStatus.Approved,
            Participants = participantIds
                .Select(id => new BookingParticipant { EmployeeId = id })
                .ToList(),
        };

        db.Bookings.Add(booking);     
        await db.SaveChangesAsync();   

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, BookingDto.From(booking));
    }

    [HttpPost("{id:int}/approve")]
    public Task<ActionResult<BookingDto>> Approve(int id) => ChangeStatusAsync(id, BookingStatus.Approved);

    [HttpPost("{id:int}/reject")]
    public Task<ActionResult<BookingDto>> Reject(int id) => ChangeStatusAsync(id, BookingStatus.Rejected);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await db.Bookings.FindAsync(id);
        if (booking is null) return NotFound();

        db.Bookings.Remove(booking);   
        await db.SaveChangesAsync();

        return NoContent();
    }


    private async Task<ActionResult<BookingDto>> ChangeStatusAsync(int id, BookingStatus status)
    {
        var booking = await db.Bookings
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return NotFound();

        if (status == BookingStatus.Approved &&
            await HasConflictAsync(booking.RoomId, booking.Date, booking.StartTime, booking.EndTime, booking.Id))
            return Conflict(new { message = "ამ დროს ოთახი უკვე დაჯავშნილია" });

        booking.Status = status;
        await db.SaveChangesAsync();   

        return Ok(BookingDto.From(booking));
    }

    private Task<bool> HasConflictAsync(
        int roomId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeId = null) =>
        db.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            b.Date == date &&
            (excludeId == null || b.Id != excludeId) &&
            b.Status != BookingStatus.Rejected &&
            start < b.EndTime &&
            end > b.StartTime);
}