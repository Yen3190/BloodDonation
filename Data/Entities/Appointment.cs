namespace VLU.BloodDonation.Api.Data.Entities;

public class Appointment
{
    public int Id{get; set; }
    public int UserId{get; set;}
    public User?User {get; set; }
    

}