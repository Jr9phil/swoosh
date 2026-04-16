using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swoosh.Api.Dtos;
using Swoosh.Api.Interfaces;
using Swoosh.Api.Security;

namespace Swoosh.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RecurringTasksController : ControllerBase
{
    private readonly IRecurringTaskService _service;

    public RecurringTasksController(IRecurringTaskService service)
    {
        _service = service;
    }

    // GET: api/recurringtasks
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(User);
        return Ok(await _service.GetAllAsync(userId));
    }

    // GET: api/recurringtasks/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        var item = await _service.GetByIdAsync(userId, id);
        return item == null ? NotFound() : Ok(item);
    }

    // POST: api/recurringtasks
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringTaskDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = UserContext.GetUserId(User);
        var item = await _service.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    // PUT: api/recurringtasks/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRecurringTaskDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = UserContext.GetUserId(User);
        return await _service.UpdateAsync(userId, id, dto) ? NoContent() : NotFound();
    }

    // DELETE: api/recurringtasks/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        return await _service.DeleteAsync(userId, id) ? NoContent() : NotFound();
    }
}
