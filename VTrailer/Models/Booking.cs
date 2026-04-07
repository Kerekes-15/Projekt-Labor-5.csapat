using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;
using System;

namespace VTrailer.Models;

[Table("Bookings")]
public class Booking : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }

    [Column("TrailerId")]
    public int TrailerId { get; set; }

    [Column("TrailerName")]
    public string? TrailerName { get; set; }

    [Column("Email")]
    public string? Email { get; set; }

    [Column("CustomerName")]
    public string? CustomerName { get; set; }

    [Column("BookingDate")]
    public DateTime BookingDate { get; set; }

    [Column("TimeSlot")]
    public string? TimeSlot { get; set; }

    [Column("TotalPrice")]
    public decimal TotalPrice { get; set; }
  
    [JsonIgnore]
    public string DisplayDate =>
        BookingTimeSlotMetadata.TryParseMultiDay(TimeSlot, out var startDate, out var endDate)
            ? $"{startDate:yyyy. MM. dd.} - {endDate:yyyy. MM. dd.}"
            : BookingDate.ToString("yyyy. MM. dd.");

    [JsonIgnore]
    public string DisplayTimeSlot => BookingTimeSlotMetadata.GetDisplayLabel(TimeSlot);

    [JsonIgnore]
    public string DisplayPrice => $"{TotalPrice:N0} Ft";
}
