namespace VLU.BloodDonation.Api.Data.Entities;

public class Appointment
{
    public int Id{get; set; }
    public int UserId{get; set;}
    public User?User {get; set; }
    
    public int BloodEventId {get; set;}
    public BloodEvent? BloodEvent { get; set;}

    public DateTime AppointmentTime {get; set; } //Gio hen hien mau
    public string Status {get; set; } = "Pending"; //pending, approved, rejected

    //Thong tin lam sang: Bac si cap nhat sau khi hien mau
    public int? BloodVolumeMl {get; set; } //Luong mau hien
    public string? CertifiedCode {get; set; } //Ma so chung nhan hien mau
    public int? PointsAwarded {get; set; } //So diem ren luyen duoc cong
    public DateTime? DonationDate {get; set; } //Ngay hien mau thuc te


}