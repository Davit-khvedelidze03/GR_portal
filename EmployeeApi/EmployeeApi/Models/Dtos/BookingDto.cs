namespace EmployeeApi.Models.Dtos;

public record BookingDto(
    int Id,
    int RoomId,
    string Title,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int OrganizerId,
    List<int> ParticipantIds,
    BookingStatus Status,
    DateTime CreatedAt)
{
    public static BookingDto From(Booking b) => new(
        b.Id,
        b.RoomId,
        b.Title,
        b.Date,
        b.StartTime,
        b.EndTime,
        b.OrganizerId,
        b.Participants.Select(p => p.EmployeeId).ToList(),
        b.Status,
        b.CreatedAt);
}