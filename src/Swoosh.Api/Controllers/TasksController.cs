using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swoosh.Api.Dtos;
using Swoosh.Api.Interfaces;
using Swoosh.Api.Security;

namespace Swoosh.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks)
    {
        _tasks = tasks;
    }

    // GET: api/tasks
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        var tasks = await _tasks.GetAllAsync(userId);
        return Ok(tasks);
    }

    // GET: api/tasks/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        var task = await _tasks.GetByIdAsync(userId, id);

        if (task == null)
            return NotFound();

        return Ok(task);
    }

    // Validates that the timer duration is within allowed bounds.
    // Returns null if valid, or a BadRequest result if invalid.
    private IActionResult? ValidateTimerDuration(int? timerDuration, DateTime? deadline)
    {
        if (timerDuration is null or 0) return null;

        if (deadline is null)
            return BadRequest(new { error = "A timer can only be set when a deadline is specified." });

        const int maxMs = 28_800_000; // 8 hours
        if (timerDuration > maxMs)
            return BadRequest(new { error = "Timer cannot exceed 8 hours (28,800,000 ms)." });

        // Timer must not cause expiry past midnight on the deadline's date.
        var midnight = deadline.Value.Date.AddDays(1);
        var expiry = deadline.Value.AddMilliseconds(timerDuration.Value);
        if (expiry > midnight)
            return BadRequest(new { error = "Timer would expire past midnight on the deadline's date." });

        return null;
    }

    // POST: api/tasks
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var timerError = ValidateTimerDuration(dto.TimerDuration, dto.Deadline);
        if (timerError != null) return timerError;

        var userId = UserContext.GetUserId(User);
        var task = await _tasks.CreateAsync(userId, dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = task.Id },
            task
        );
    }

    // PUT: api/tasks/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var timerError = ValidateTimerDuration(dto.TimerDuration, dto.Deadline);
        if (timerError != null) return timerError;

        var userId = UserContext.GetUserId(User);

        bool updated;
        try
        {
            updated = await _tasks.UpdateAsync(userId, id, dto);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // POST: api/tasks/{parentId}/subtasks
    [HttpPost("{parentId:guid}/subtasks")]
    public async Task<IActionResult> CreateSubtask(Guid parentId, [FromBody] CreateSubtaskDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var timerError = ValidateTimerDuration(dto.TimerDuration, dto.Deadline);
        if (timerError != null) return timerError;

        var userId = UserContext.GetUserId(User);

        var parent = await _tasks.GetByIdAsync(userId, parentId);
        if (parent == null)
            return NotFound();

        var subtask = await _tasks.CreateSubtaskAsync(userId, parentId, dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = subtask.Id },
            subtask
        );
    }

    // PATCH: api/tasks/{id}/order
    [HttpPatch("{id:guid}/order")]
    public async Task<IActionResult> Reorder(Guid id, [FromBody] ReorderTaskDto dto)
    {
        var userId = UserContext.GetUserId(User);
        var result = await _tasks.ReorderAsync(userId, id, dto.Modified);
        return result ? NoContent() : NotFound();
    }

    // PUT: api/tasks/{childId}/parent/{parentId}
    [HttpPut("{childId:guid}/parent/{parentId:guid}")]
    public async Task<IActionResult> AttachToParent(Guid childId, Guid parentId)
    {
        var userId = UserContext.GetUserId(User);
        var result = await _tasks.AttachToParentAsync(userId, childId, parentId);
        return result ? NoContent() : NotFound();
    }

    // DELETE: api/tasks/{id}/parent
    [HttpDelete("{id:guid}/parent")]
    public async Task<IActionResult> DetachFromParent(Guid id, [FromBody] DetachFromParentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = UserContext.GetUserId(User);
        var result = await _tasks.DetachFromParentAsync(userId, id, dto.Priority, dto.Modified);
        return result ? NoContent() : NotFound();
    }

    // DELETE: api/tasks/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        var deleted = await _tasks.DeleteAsync(userId, id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
