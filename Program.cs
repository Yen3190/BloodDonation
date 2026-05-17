using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VLU.BloodDonation.Api.Data;

var builder = WebApplication.CreateBuilder(args);

//Cau hinh dich vu
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//Cau hinh pipeline/middleware

if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VLU Blood Donation Network API v1");
    });
}

app.UseHttpsRedirection(); //Tu dong chuyen huong cac request sang giao thuc bao mat HTTPS

app.UseAuthorization(); //Kich hoat tinh nang phan quyen

app.MapControllers(); //Ban do hoa Controllers

app.Run();