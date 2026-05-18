using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VLU.BloodDonation.Api.Data;
using VLU.BloodDonation.Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;

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
    [Authorize(Policy ="RequireAdmin")]
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
        newUser.Role = "Student";

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Tạo tài khoản sinh viên thành công!",
            UserId = newUser.Id
        });
    }

    [HttpPost("sso-staff-register")]
    public async Task<IActionResult> RegisterStaffViaSSO([FromBody] User ssoUser)
    {
        // Kiểm tra xem tài khoản cán bộ này đã tồn tại trong DB chưa
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == ssoUser.Email);

        if (existingUser != null)
        {
            // Nếu đã tồn tại nhưng chưa được Admin duyệt
            if (!existingUser.IsApproved)
            {
                return StatusCode(403, new { Message = "Tài khoản SSO của bạn đang chờ Admin phê duyệt quyền hạn." });
            }
            return Ok(new { Message = "Đăng nhập thành công!", User = existingUser });
        }

        // Nếu là tài khoản cán bộ đăng nhập lần đầu tiên qua SSO
        ssoUser.DateCreated = DateTime.Now;
        ssoUser.TotalPoints = 0;
        ssoUser.Role = "Staff";       // Tự động nhận diện diện cán bộ/nhân viên
        ssoUser.IsApproved = false;   // Trạng thái chờ duyệt

        _context.Users.Add(ssoUser);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAllUsers), new { id = ssoUser.Id }, new
        {
            Message = "Đăng ký qua Microsoft SSO thành công! Vui lòng đợi Admin phê duyệt để kích hoạt quyền Staff."
        });
    }

    // 4. API lấy danh sách cán bộ Staff đang chờ phê duyệt (Dành cho Admin)
    [HttpGet("pending-staff")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetPendingStaff()
    {
        var pendingList = await _context.Users
            .Where(u => u.Role == "Staff" && u.IsApproved == false)
            .ToListAsync();

        return Ok(pendingList);
    }

    // 5. API Ph phê duyệt tài khoản Staff hoạt động chính thức (Dành cho Admin)
    [HttpPut("approve-staff/{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> ApproveStaff(int id)
    {
        var staffUser = await _context.Users.FindAsync(id);
        if (staffUser == null) return NotFound("Không tìm thấy tài khoản nhân sự này.");

        if (staffUser.Role != "Staff" && staffUser.Role != "Doctor")
        {
            return BadRequest("Tài khoản này không thuộc nhóm đối tượng nhân sự y tế/Staff.");
        }

        // Kích hoạt tài khoản
        staffUser.IsApproved = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Đã phê duyệt thành công! Cán bộ [{staffUser.FullName}] hiện đã có toàn quyền Staff."
        });
    }

}