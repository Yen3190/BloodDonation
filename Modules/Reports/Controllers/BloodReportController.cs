using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using VLU.BloodDonation.Api.Data;
using OfficeOpenXml.FormulaParsing.Excel;

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
}