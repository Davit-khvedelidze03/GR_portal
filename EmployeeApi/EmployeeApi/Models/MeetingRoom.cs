namespace EmployeeApi.Models;

public class MeetingRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public List<string> Equipment { get; set; } = new();

    public bool RequiresApproval { get; set; }
}
