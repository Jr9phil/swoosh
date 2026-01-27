using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;

namespace Swoosh.Api.Controllers;

[ApiController]
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
        var tasks = await _db.Tasks.ToListAsync();
        return Ok(tasks);
    }
    
    // GET: api/tasks/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        return  Ok(task);
    }
    
    // POST: api/tasks
    [HttpPost]
    public async Task<IActionResult> Create(TaskItem task)
    {
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }
    
    // PUT: api/tasks/{id}
    [HttpPut("{id}")]
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
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }
        
}