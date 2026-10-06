namespace EmployeeApi.Models.Dtos;

public record EmployeeDto(int Id,
    string Name,
    string Position,
    string Email,
    string? PhotoUrl,
    bool ShowInDirectory)
{
    public static EmployeeDto From(Employee e) => new(
        e.Id,
        e.Name,
        e.Position,
        e.Email,
        e.PhotoFileName is null ? null : $"/uploads/employees/{e.PhotoFileName}",
        e.ShowPhotoInDirectory);
}