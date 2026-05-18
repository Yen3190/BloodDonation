using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VLU.BloodDonation.Api.Data;
using VLU.BloodDonation.Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace VLU.BloodDonation.Api.Modules.Users.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration; // Khai báo cấu hình hệ thống

    // Cập nhật Constructor để nhận IConfiguration phục vụ cho việc tạo JWT
    public UserController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // 1. API lấy danh sách toàn bộ người dùng/sinh viên
    [HttpGet]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    // 2. API đăng ký tài khoản sinh viên
    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser([FromBody] User newUser)
    {
        var isExist = await _context.Users.AnyAsync(u => u.StudentId == newUser.StudentId || u.Email == newUser.Email);
        if (isExist) return BadRequest("Mã số sinh viên hoặc Email đã tồn tại trong hệ thống.");

        newUser.DateCreated = DateTime.Now;
        newUser.TotalPoints = 0;
        newUser.Role = "Student";
        newUser.IsApproved = true;

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Tạo tài khoản sinh viên thành công!",
            UserId = newUser.Id
        });
    }

    // 3. API Đăng ký lần đầu hoặc Đăng nhập qua Microsoft SSO dành cho Staff
    [HttpPost("sso-staff-register")]
    public async Task<IActionResult> RegisterStaffViaSSO([FromBody] User ssoUser)
    {
        // Tìm xem email cán bộ này đã có trong hệ thống chưa
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == ssoUser.Email);

        if (existingUser != null)
        {
            // Nếu tài khoản đã tồn tại nhưng cột IsApproved dưới DB vẫn đang là false (0)
            if (!existingUser.IsApproved)
            {
                return StatusCode(403, new
                {
                    Message = "Tài khoản SSO của bạn đang trong danh sách chờ Admin phê duyệt quyền hạn."
                });
            }
            return Ok(new { Message = "Đăng nhập hệ thống thành công!", User = existingUser });
        }

        // Trường hợp cán bộ đăng nhập SSO lần đầu tiên: Lưu vào DB và bắt buộc chờ duyệt
        ssoUser.DateCreated = DateTime.Now;
        ssoUser.TotalPoints = 0;
        ssoUser.Role = "Staff";
        ssoUser.IsApproved = false;

        _context.Users.Add(ssoUser);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAllUsers), new { id = ssoUser.Id }, new
        {
            Message = "Đăng ký qua Microsoft SSO thành công! Vui lòng đợi Admin phê duyệt để kích hoạt quyền Staff."
        });
    }

    // 4. API lấy danh sách cán bộ Staff đang chờ phê duyệt
    [HttpGet("pending-staff")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetPendingStaff()
    {
        // Quét DB xem tài khoản nào mang quyền Staff và cột IsApproved đang là false
        var pendingList = await _context.Users
            .Where(u => u.Role == "Staff" && u.IsApproved == false)
            .ToListAsync();

        return Ok(pendingList);
    }

    // 5. API Phê duyệt kích hoạt tài khoản Staff hoạt động chính thức (Chỉ Admin)
    [HttpPut("approve-staff/{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> ApproveStaff(int id)
    {
        var staffUser = await _context.Users.FindAsync(id);
        if (staffUser == null) return NotFound("Không tìm thấy tài khoản nhân sự này.");

        if (staffUser.Role != "Staff" && staffUser.Role != "Doctor")
        {
            return BadRequest("Tài khoản được chọn không phải là nhân sự y tế (Staff/Doctor).");
        }
        staffUser.IsApproved = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Đã phê duyệt thành công! Cán bộ [{staffUser.FullName}] hiện đã có toàn quyền Staff để nhập liệu dữ liệu lâm sàng."
        });
    }

    // 6. API đăng nhập để lấy Token thực tế phục vụ kiểm thử hệ thống
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        // Kiểm định sự tồn tại của tài khoản dựa trên Email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null || user.PasswordHash != loginDto.PasswordHash)
        {
            return Unauthorized("Email hoặc mật khẩu không chính xác.");
        }

        // Nếu là tài khoản chưa được phê duyệt kích hoạt thì không cho phép đăng nhập
        if (!user.IsApproved)
        {
            return StatusCode(403, "Tài khoản của bạn đang chờ Admin phê duyệt.");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName ?? ""),
            new Claim(ClaimTypes.Role, user.Role ?? "Student"),
            new Claim("StudentId", user.StudentId ?? "")
        };

        // Lấy chuỗi khóa bảo mật để tiến hành mã hóa Token
        var jwtKey = _configuration["Jwt:Key"] ?? "7f9b8c2d-4e5a-6f7b-8c9d-0e1f2a3b4c5dsherryvinyard";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Ok(new
        {
            Message = "Đăng nhập thành công!",
            AccessToken = tokenHandler.WriteToken(token),
            User = new { user.Id, user.FullName, user.Email, user.Role }
        });
    }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}