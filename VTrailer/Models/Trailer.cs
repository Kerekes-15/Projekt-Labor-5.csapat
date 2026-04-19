using Supabase.Postgrest.Attributes; 
using Supabase.Postgrest.Models;     

namespace VTrailer.Models;

[Table("Trailers")]
public class Trailer : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }

    [Column("LicensePlate")]
    public string? LicensePlate { get; set; }

    [Column("Category")]
    public string? Category { get; set; }

    [Column("BrandAndModel")]
    public string? BrandAndModel { get; set; }

    [Column("PayloadCapacityKg")]
    public int PayloadCapacityKg { get; set; }

    [Column("TotalWeightKg")]
    public int TotalWeightKg { get; set; }

    [Column("InnerLengthCm")]
    public double InnerLengthCm { get; set; }

    [Column("InnerWidthCm")]
    public double InnerWidthCm { get; set; }

    [Column("DailyRateFt")]
    public decimal DailyRateFt { get; set; }

    [Column("DepositFt")]
    public decimal DepositFt { get; set; }

    [Column("Status")]
    public string? Status { get; set; }

    [Column("ImageUrl")]
    public string? ImageUrl { get; set; }

    public string DailyRateFormatted => $"{DailyRateFt:N0}";
    public string DepositFormatted => $"{DepositFt:N0}";
}
