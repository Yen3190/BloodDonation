using Microsoft.EntityFrameworkCore;
using VLU.BloodDonation.Api.Data;
using OfficeOpenXml;


ExcelPackage.License.SetNonCommercialPersonal("VLU");

// 2. Khởi tạo Builder (Chỉ khai báo DUY NHẤT một lần ở đây)
var builder = WebApplication.CreateBuilder(args);

// Cấu hình Database MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Kích hoạt Controller để tiếp nhận API kết xuất Excel
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

// Định tuyến các API Endpoint
app.MapControllers();

app.Run();