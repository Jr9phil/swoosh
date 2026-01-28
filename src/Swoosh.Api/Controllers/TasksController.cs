using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Dtos;
using Swoosh.Api.Security;

namespace Swoosh.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }
    
    // GET: api/tasks
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);

        var tasks = await _db.Tasks
            .Where(t => t.UserId == userId)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.EncryptedTitle,
                Notes = t.EncryptedNotes,
                Deadline = t.Deadline,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(tasks);
    }
    
    // GET: api/tasks/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = UserContext.GetUserId(User);

        var task = await _db.Tasks
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.EncryptedTitle,
                Notes = t.EncryptedNotes,
                Deadline = t.Deadline,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (task == null) return NotFound();
        return Ok(task);
    }
    
    // POST: api/tasks
    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required");

        var userId = UserContext.GetUserId(User);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EncryptedTitle = dto.Title, // encryption later
            EncryptedNotes = dto.Notes,
            Deadline = dto.Deadline,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, new TaskDto
        {
            Id = task.Id,
            Title = dto.Title,
            Notes = dto.Notes,
            Deadline = task.Deadline,
            IsCompleted = task.IsCompleted,
            CreatedAt = task.CreatedAt
        });
    }
    
    // PUT: api/tasks/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskDto dto)
    {
        var userId = UserContext.GetUserId(User);

        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound();

        // TODO: Encrypt before saving once crypto is added
        task.EncryptedTitle = dto.Title;
        task.EncryptedNotes = dto.Notes;
        task.Deadline = dto.Deadline;
        task.IsCompleted = dto.IsCompleted;

        await _db.SaveChangesAsync();

        return NoContent();
    }
    
    // DELETE: api/tasks/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        
        var task = await _db.Tasks
            .Where(t => t.Id == id && t.UserId == userId)
            .FirstOrDefaultAsync();
        if (task == null) return NotFound();
        
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }
        
}