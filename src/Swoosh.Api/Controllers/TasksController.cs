using Microsoft.AspNetCore.Mvc;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;

namespace Swoosh.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{userId}")]
    public IActionResult GetTasks(Guid userId)
    {
        var tasks = _db.Tasks
            .Where(t => t.UserId == userId)
            .ToList();

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TaskItem task)
    {
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return Ok(task);
    }
}