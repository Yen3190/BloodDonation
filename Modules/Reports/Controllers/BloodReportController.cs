using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using VLU.BloodDonation.Api.Data;
using OfficeOpenXml.FormulaParsing.Excel;
using VLU.BloodDonation.Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;

namespace VLU.BloodDonation.Api.Modules.Reports.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BloodReportController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public BloodReportController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("export-donors")]
    [Authorize(Policy = "RequireAdmin")] //Chi admin duoc xuat ds
    public async Task<IActionResult> ExportDonorsToExcel()
    {
        //1. Lay ds sinh vien hien mau thanh cong (Status = Attended)
        var donorList = await _context.Appointments
            .Include(a => a.User)
            .Include(a => a.BloodEvent)
            .Where(a => a.Status== "Attended")
            .OrderByDescending(a => a.DonationDate)
            .ToListAsync();

        //2. Khoi tao Package Excel qua EPPlus
        //Da khai bao chung o appsetting.json
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Danh sách cấp chứng nhận");
        
        //3. Tao tieu de cho bang trong excel
        string [] headers = {"STT", "MSSV", "Họ và Tên", "Đợt hiến máu", "Nhóm máu", "Thể tích (ml)", "Điểm cộng", "Mã chứng nhận", "Ngày hiến"};

        for(int i = 0; i < headers.Length; i++)
        {
            var cell  = sheet.Cells[1, i + 1];
            cell.Value =  headers[i];

            cell.Style.Font.Bold = true;
            cell.Style.Font.Name = "Times New Roman";
            cell.Style.Font.Size = 13;
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        }

        //4. Do du lieu tu database vao cac hang tiep theo trong bang
        int rowIndex = 2;
        int stt = 1;
        foreach (var item in donorList)
        {
            sheet.Cells[rowIndex, 1].Value = stt++;
            sheet.Cells[rowIndex, 2].Value = item.User?.StudentId ?? "N/A";
            sheet.Cells[rowIndex, 3].Value = item.User?.FullName ?? "N/A";
            sheet.Cells[rowIndex, 4].Value = item.BloodEvent?.Name ?? "N/A";
            sheet.Cells[rowIndex, 5].Value =  $"{item.User?.BloodType}{item.User?.RhFactor}";
            sheet.Cells[rowIndex, 6].Value = item.BloodVolumeMl;
            sheet.Cells[rowIndex, 7].Value = item.PointsAwarded;
            sheet.Cells[rowIndex, 8].Value = item.CertifiedCode ?? "Chưa cấp";
            sheet.Cells[rowIndex, 9].Value = item.DonationDate?.ToString("dd/MM/yyyy");

            sheet.Cells[rowIndex, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[rowIndex, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[rowIndex, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[rowIndex, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[rowIndex, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[rowIndex, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[rowIndex, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[rowIndex, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[rowIndex, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            rowIndex++;
        }

        //5. Tu dong can chinh do rong cot va ke vien bang 
        if (sheet.Dimension != null)
        {
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            int totalRows = sheet.Dimension.End.Row;
            int totalCols = sheet.Dimension.End.Column;

            using (var range = sheet.Cells[1, 1, totalRows, totalCols])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }
        }

        //6. Xuat mang byte va tra ve file excel dinh dang openXML
        var fileBytes = package.GetAsByteArray();
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhSachChungNhanHienMau.xlsx");
    }

    
    // Định nghĩa cấu trúc dữ liệu người dùng gửi lên khi xác nhận hiến máu
    public class ConfirmDonationDto
    {
        public int UserId { get; set; }
        public int BloodEventId { get; set; }
        public int BloodVolumeMl { get; set; } // Lượng máu hiến 
        public int PointsAwarded { get; set; }  // Điểm cộng 
    }

    // API SỬA ĐỔI: Ghi nhận hiến máu + Tự động cộng dồn điểm cho sinh viên
    [HttpPost("confirm-donation")]
    [Authorize(Policy = "RequireStaff")]
    public async Task<IActionResult> ConfirmDonation([FromBody] ConfirmDonationDto dto)
    {
        // 1. Kiểm tra sự tồn tại của sinh viên và đợt hiến máu
        var studentUser = await _context.Users.FindAsync(dto.UserId);
        if (studentUser == null) return NotFound("Không tìm thấy thông tin sinh viên trên hệ thống.");

        var bloodEvent = await _context.BloodEvents.FindAsync(dto.BloodEventId);
        if (bloodEvent == null) return NotFound("Không tìm thấy đợt hiến máu này.");

        // 2. Tạo ngẫu nhiên một Mã chứng nhận hiến máu (VD: VLU-BL-XXXXXX)
        var random = new Random();
        string certifiedCode = $"VLU-BL-{random.Next(100000, 999999)}";

        // 3. Kiểm tra xem sinh viên có lịch hẹn "Pending" nào ở đợt này chưa
        var existingAppointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.UserId == dto.UserId && a.BloodEventId == dto.BloodEventId && a.Status == "Pending");

        if (existingAppointment != null)
        {
            // Nếu đã đặt lịch trước: Cập nhật trực tiếp trên bản ghi đó
            existingAppointment.Status = "Attended";
            existingAppointment.BloodVolumeMl = dto.BloodVolumeMl;
            existingAppointment.CertifiedCode = certifiedCode;
            existingAppointment.PointsAwarded = dto.PointsAwarded;
            existingAppointment.DonationDate = DateTime.Now;
        }
        else
        {
            // Nếu sinh viên vãng lai chưa đặt lịch trước: Tạo mới bản ghi Attended
            var newAppointment = new Appointment
            {
                UserId = dto.UserId,
                BloodEventId = dto.BloodEventId,
                AppointmentTime = DateTime.Now,
                Status = "Attended",
                BloodVolumeMl = dto.BloodVolumeMl,
                CertifiedCode = certifiedCode,
                PointsAwarded = dto.PointsAwarded,
                DonationDate = DateTime.Now
            };
            _context.Appointments.Add(newAppointment);
        }
        studentUser.TotalPoints += dto.PointsAwarded;

        // 5. Lưu tất cả thay đổi xuống Database (Cả bảng Appointments và bảng Users)
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Ghi nhận thành công! Đã cấp mã [{certifiedCode}] và cộng thêm {dto.PointsAwarded} điểm rèn luyện cho sinh viên [{studentUser.FullName}].",
            CurrentTotalPoints = studentUser.TotalPoints
        });
    }

    // Định nghĩa cấu trúc dữ liệu khi sinh viên đăng ký đặt lịch hẹn trước
    public class RegisterAppointmentDto
    {
        public int BloodEventId { get; set; }
        public DateTime AppointmentTime { get; set; } // Giờ hẹn đến hiến (VD: 08:30 AM)
    }

    // Cho phép Sinh viên tự đặt lịch hẹn trước từ giao diện App
    [HttpPost("register-appointment")]
    [Authorize]
    public async Task<IActionResult> RegisterAppointment([FromBody] RegisterAppointmentDto dto)
    {
        // 1. Lấy Id của Sinh viên đang đăng nhập từ dữ liệu Claim trong Token JWT
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized("Phiên đăng nhập không hợp lệ.");
        int currentUserId = int.Parse(userIdClaim.Value);

        // 2. Kiểm tra xem đợt hiến máu này có tồn tại hay không
        var bloodEvent = await _context.BloodEvents.FindAsync(dto.BloodEventId);
        if (bloodEvent == null) return NotFound("Đợt hiến máu được chọn không tồn tại.");

        // 3. Kiểm tra xem sinh viên này đã đăng ký lịch hẹn cho đợt này chưa (Tránh đăng ký trùng)
        var isRegistered = await _context.Appointments
            .AnyAsync(a => a.UserId == currentUserId && a.BloodEventId == dto.BloodEventId);
        if (isRegistered) return BadRequest("Bạn đã đăng ký lịch hẹn cho đợt hiến máu này rồi.");

        // 4. Khởi tạo lịch hẹn với trạng thái ban đầu là "Pending"
        var appointment = new Appointment
        {
            UserId = currentUserId,
            BloodEventId = dto.BloodEventId,
            AppointmentTime = dto.AppointmentTime,
            Status = "Pending",
            BloodVolumeMl = 0,   
            PointsAwarded = 0    
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Đặt lịch hẹn hiến máu thành công! Vui lòng đến đúng giờ hẹn." });
    }

}