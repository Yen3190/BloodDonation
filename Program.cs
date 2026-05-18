using Microsoft.EntityFrameworkCore;
using VLU.BloodDonation.Api.Data;
using OfficeOpenXml;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

ExcelPackage.License.SetNonCommercialPersonal("VLU");

// 1. Khởi tạo Builder
var builder = WebApplication.CreateBuilder(args);

// 2.Cấu hình Database MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

//3. Cấu hình hệ thống xác thực bằng token
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "Chuoi_Secret_Key_Mac_Dinh_Cua_Yen"))
    };
});

// 4. Cấu hình chính sách phân quyền (Role + quyền hạn)
builder.Services.AddAuthorization(options =>
{
    // Yêu cầu người dùng phải mang Role là Admin mới có quyền truy cập
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    // Yêu cầu người dùng phải thuộc nhóm nhân sự quản lý (Admin hoặc Bác sĩ/Nhân viên)
    options.AddPolicy("RequireStaff", policy => policy.RequireRole("Admin", "Staff", "Doctor"));
});


// 5. Kích hoạt Controller để nhận API
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication(); //Xác thực danh tính
app.UseAuthorization(); //Xác thực role (quyền)

// Định tuyến các API Endpoint
app.MapControllers();

app.Run();