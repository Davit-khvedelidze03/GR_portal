namespace EmployeeApi.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhotoFileName { get; set; } 
    public bool ShowPhotoInDirectory { get; set; } = true;
}