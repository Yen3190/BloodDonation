namespace VLU.BloodDonation.Api.Data.Entities;

public class BloodStock
{
    public int Id { get; set; }
    public string BloodType {get; set; } = string.Empty;
    public string RhFactor {get; set; } = string.Empty;
    public int TotalVolumeMl {get; set; }
    public DateTime LastUpdated {get; set; } = DateTime.UtcNow;
}