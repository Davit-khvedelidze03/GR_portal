using EmployeeApi.Data;
using EmployeeApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private const long MaxPhotoSize = 10 * 1024 * 1024;   // 10 MB

    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"]  = ".png",
        ["image/webp"] = ".webp",
    };

    private string PhotosFolder =>
        Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "employees");
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll()
    {
        var employees = await db.Employees.AsNoTracking().OrderBy(e => e.Id).ToListAsync();
        return Ok(employees.Select(EmployeeDto.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return employee is null ? NotFound() : Ok(EmployeeDto.From(employee));
    }

    [HttpPost("{id:int}/photo")]
    public async Task<ActionResult<EmployeeDto>> UploadPhoto(int id, IFormFile? file)
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee is null)
        {
            return NotFound(new { message = "ტანამშრომელი ვერ მოიძებნა" });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "ფაილი არ არის არჩეული" });
        }

        if (file.Length > MaxPhotoSize)
        {
            return BadRequest(new { message =  $"ფოტოს ზომა არ უნდა აღემატებოდეს {MaxPhotoSize / 1024 / 1024} MB-ს" });
        }

        if (!AllowedTypes.TryGetValue(file.ContentType, out var extension))
        {
            return BadRequest( new { message = "მხოლოდ jpeg, png და webp ტიპის ფაილებია დაშვებული" });
        }

        Directory.CreateDirectory(PhotosFolder);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(PhotosFolder, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var oldFileName = employee.PhotoFileName;
        employee.PhotoFileName = fileName;
        await db.SaveChangesAsync();

        DeletePhotoFile(oldFileName);
        
        return Ok(EmployeeDto.From(employee));
    }

    [HttpDelete("{id:int}/photo")]
    public async Task<ActionResult<EmployeeDto>> DeletePhoto(int id)
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee is null)
        {
            return NotFound(new { message = "ტანამშრომელი ვერ მოიძებნა" });
        }

        var oldFileName = employee.PhotoFileName;
        employee.PhotoFileName = null;
        await db.SaveChangesAsync();

        DeletePhotoFile(oldFileName);
        
        return Ok(EmployeeDto.From(employee));
    }

    [HttpPut("{id:int}/photo/visibility")]
    public async Task<ActionResult<EmployeeDto>> SetPhotoVisibility(int id, PhotoVisibilityRequest request)
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee is null)
            return NotFound(new { message = "თანამშრომელი ვერ მოიძებნა" });

        employee.ShowPhotoInDirectory = request.ShowInDirectory;
        await db.SaveChangesAsync();

        return Ok(EmployeeDto.From(employee));
    }

    private void DeletePhotoFile(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return;

        var path = Path.Combine(PhotosFolder, fileName);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}