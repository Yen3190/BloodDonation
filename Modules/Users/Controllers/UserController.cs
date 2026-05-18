using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VLU.BloodDonation.Api.Data;
using VLU.BloodDonation.Api.Data.Entities;

namespace VLU.BloodDonation.Api.Modules.Users.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    // 1. api lấy danh sách toàn bộ người dùng/sinh viên
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    // 2. api đăng ký/tạo mới tài khoản sinh viên
    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser([FromBody] User newUser)
    {
        // Kiểm tra trùng lặp mssv và email
        var isExist = await _context.Users.AnyAsync(u => u.StudentId == newUser.StudentId || u.Email == newUser.Email);
        if (isExist) return BadRequest("Mã số sinh viên hoặc Email đã tồn tại trong hệ thống.");

        newUser.DateCreated = DateTime.Now;
        newUser.TotalPoints = 0; // Điểm ban đầu bằng 0

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Tạo tài khoản sinh viên thành công!",
            UserId = newUser.Id
        });
    }
}