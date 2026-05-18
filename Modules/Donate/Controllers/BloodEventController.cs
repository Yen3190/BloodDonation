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

    // 3. API cập nhật thông tin đợt hiến máu (Chỉ Admin)
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] BloodEvent updatedEvent)
    {
        // Tìm đợt hiến máu cũ trong DB dựa vào ID trên đường dẫn URL
        var existingEvent = await _context.BloodEvents.FindAsync(id);
        if (existingEvent == null) return NotFound("Không tìm thấy đợt hiến máu này để cập nhật.");

        // Tiến hành cập nhật các trường thông tin mới do Admin gửi lên
        existingEvent.Name = updatedEvent.Name;
        existingEvent.Location = updatedEvent.Location;
        existingEvent.StartDate = updatedEvent.StartDate;
        existingEvent.EndDate = updatedEvent.EndDate;
        existingEvent.Description = updatedEvent.Description;

        // Lưu thay đổi xuống Database
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Cập nhật thông tin đợt hiến máu thành công!",
            Data = existingEvent
        });
    }

    // 4. API xóa bỏ một đợt hiến máu lỗi/hủy lịch (Chỉ Admin)
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var bloodEvent = await _context.BloodEvents.FindAsync(id);
        if (bloodEvent == null) return NotFound("Không tìm thấy đợt hiến máu này trên hệ thống.");

        // Kiểm tra xem đợt hiến máu này đã có sinh viên nào đăng ký lịch hẹn chưa
        var hasAppointments = await _context.Appointments.AnyAsync(a => a.BloodEventId == id);
        if (hasAppointments)
        {
            return BadRequest("Không thể xóa đợt hiến máu này vì đã có sinh viên đăng ký tham gia lịch hẹn.");
        }

        // Nếu chưa có ai đăng ký thì cho phép xóa hoàn toàn khỏi DB
        _context.BloodEvents.Remove(bloodEvent);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Đã xóa đợt hiến máu thành công khỏi hệ thống!" });
    }
}