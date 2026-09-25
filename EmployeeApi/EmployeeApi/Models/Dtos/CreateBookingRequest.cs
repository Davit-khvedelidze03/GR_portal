using System.ComponentModel.DataAnnotations;

namespace EmployeeApi.Models.Dtos;

public class CreateBookingRequest
{
    [Required, Range(1, int.MaxValue)]
    public int RoomId { get; set; }

    [Required, StringLength(120, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int OrganizerId { get; set; }

    public List<int> ParticipantIds { get; set; } = new();
}
