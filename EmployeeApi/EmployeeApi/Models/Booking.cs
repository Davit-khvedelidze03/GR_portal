namespace EmployeeApi.Models;

public enum BookingStatus
{
    Pending,
    Approved,
    Rejected
}

public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int OrganizerId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<BookingParticipant> Participants { get; set; } = new();
}
