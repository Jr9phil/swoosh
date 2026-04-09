using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swoosh.Api.Dtos;
using Swoosh.Api.Interfaces;
using Swoosh.Api.Security;

namespace Swoosh.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _service;

    public RemindersController(IReminderService service)
    {
        _service = service;
    }

    // GET: api/reminders
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        return Ok(await _service.GetAllAsync(userId));
    }

    // GET: api/reminders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        var item = await _service.GetByIdAsync(userId, id);
        return item == null ? NotFound() : Ok(item);
    }

    // POST: api/reminders
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReminderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = UserContext.GetUserId(User);
        var item = await _service.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    // PUT: api/reminders/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReminderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = UserContext.GetUserId(User);
        return await _service.UpdateAsync(userId, id, dto) ? NoContent() : NotFound();
    }

    // DELETE: api/reminders/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        return await _service.DeleteAsync(userId, id) ? NoContent() : NotFound();
    }
}
