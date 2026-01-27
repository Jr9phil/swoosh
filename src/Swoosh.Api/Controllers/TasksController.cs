using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Dtos;

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
        //This only exists for testing purposes and will be removed
        var tasks = await _db.Tasks
            .Select( t => new TaskDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Title = t.EncryptedTitle,
                Notes = t.EncryptedNotes,
                Deadline = t.Deadline,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
        return Ok(tasks);
    }
    
    // GET: api/tasks/user/{userId}
    [HttpGet("/user/{userId:guid}")]
    public async Task<IActionResult> GetUserTasks(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if(user == null) return BadRequest("User not found");
            
        var tasks = await _db.Tasks
            .Where(t => t.UserId == userId)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                UserId = t.UserId,
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
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();

        var taskDto = new TaskDto
        {
            Id = task.Id,
            UserId = task.UserId,
            Title = task.EncryptedTitle,
            Notes = task.EncryptedNotes,
            Deadline = task.Deadline,
            IsCompleted = task.IsCompleted,
            CreatedAt = task.CreatedAt
        };
        return  Ok(taskDto);
    }
    
    // POST: api/tasks
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        if(string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required");
        
        var user = await _db.Users.FindAsync(dto.UserId);
        if(user == null) return BadRequest("User not found");
        
        //TODO: Add encryption
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            EncryptedTitle = dto.Title,
            EncryptedNotes = dto.Notes,
            Deadline = dto.Deadline,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        var taskDto = new TaskDto
        {
            Id = task.Id,
            UserId = task.UserId,
            Title = dto.Title,
            Notes = dto.Notes,
            Deadline = task.Deadline,
            IsCompleted = task.IsCompleted,
            CreatedAt = task.CreatedAt
        };
        
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, taskDto);
    }
    
    // PUT: api/tasks/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TaskItem updatedTask)
    {
        if (id != updatedTask.Id) return BadRequest();

        _db.Entry(updatedTask).State = EntityState.Modified;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if(!_db.Tasks.Any(t => t.Id == id)) return NotFound();
            throw;
        }
        
        return NoContent();
    }
    
    // DELETE: api/tasks/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }
        
}