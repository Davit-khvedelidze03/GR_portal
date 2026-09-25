using EmployeeApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private static readonly List<Employee> Employees = new()
    {
        new Employee { Id = 1, Name = "Davit Khvedelidze", Position = "Manager", Email = "davit.khvedelidze@example.com" },
        new Employee { Id = 2, Name = "Giorgi Beridze", Position = "Developer", Email = "giorgi.beridze@example.com" },
        new Employee { Id = 3, Name = "Nino Kapanadze", Position = "Designer", Email = "nino.kapanadze@example.com" },
    };

    [HttpGet]
    public ActionResult<IEnumerable<Employee>> GetAll()
    {
        return Ok(Employees);
    }

    [HttpGet("{id}")]
    public ActionResult<Employee> GetById(int id)
    {
        var employee = Employees.FirstOrDefault(e => e.Id == id);
        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }
}