using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VLU.BloodDonation.Api.Data;
using VLU.BloodDonation.Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;

namespace VLU.BloodDonation.Api.Modules.Donate.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BloodEventController : ControllerBase
{
    private readonly AppDbContext _context;

    public BloodEventController(AppDbContext context)
    {
        _context = context;
    }

    // 1. api lấy danh sách tất cả các đợt hiến máu
    [HttpGet]
    public async Task<IActionResult> GetAllEvents()
    {
        var events = await _context.BloodEvents.ToListAsync();
        return Ok(events);
    }

    // 2. api tạo mới một đợt hiến máu (func của admin)
    [HttpPost]
    [Authorize(Policy ="RequireAdmin")] //Chi admin co quyen tao dot hien mau
    public async Task<IActionResult> CreateEvent([FromBody] BloodEvent newEvent)
    {
        if (newEvent == null) return BadRequest("Dữ liệu không hợp lệ.");

        _context.BloodEvents.Add(newEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAllEvents), new { id = newEvent.Id }, new
        {
            Message = "Tạo đợt hiến máu thành công!",
            Data = newEvent
        });
    }
}