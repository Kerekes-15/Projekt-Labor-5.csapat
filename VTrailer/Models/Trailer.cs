using SQLite;

namespace VTrailer.Models;

public class Trailer
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string? LicensePlate { get; set; }
    public string? Category { get; set; }
    public string? BrandAndModel { get; set; }
    public int PayloadCapacityKg { get; set; }
    public int TotalWeightKg { get; set; }
    public double InnerLengthCm { get; set; }
    public double InnerWidthCm { get; set; }
    public decimal DailyRateFt { get; set; }
    public decimal DepositFt { get; set; }
    public string? Status { get; set; }
    public string? ImageUrl { get; set; }
}
