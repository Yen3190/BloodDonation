namespace VLU.BloodDonation.Api.Data.Entities;

public class BloodEvent
{
    public int Id {get; set; }
    public string Name {get; set; } = string.Empty; //Ten su kien hien mau
    public string Location {get; set; } = string.Empty; //Dia diem
    public DateTime StartDate {get; set; }
    public DateTime EndDate {get; set; }
    public int TargetDonors {get; set; } //Chi tieu hien mau
    public string Status {get; set; } = "Upcoming"; //Upcoming, Active, Completed

    //Quan he
    public ICollection<Appointment> Appointments {get; set; } = new List<Appointment>();
}