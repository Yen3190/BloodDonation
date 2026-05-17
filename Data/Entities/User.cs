namespace VLU.BloodDonation.Api.Data.Entities;

public class User
{
    public int Id {get; set;}
    public string StudentId {get; set; } = string.Empty; //MSSV
    public string FullName {get; set; } = string.Empty;
    public string Email {get; set; } = string.Empty;
    public string PasswordHash {get; set; } = string.Empty;
    public string Role {get; set; } = "Student"; //3 role: Student, Doctor và Admin
    public string? BloodType {get; set; } //O, A, B, AB
    public string? RhFactor {get; set; } //+ hay -
    public int TotalPoints {get; set; } //Điểm rèn luyện sẽ nhận được
    public DateTime DateCreated {get; set; } = DateTime.UtcNow;

    //Quan hệ: 1 Sv có thể đăng ký nhiều lịch hẹn
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

}