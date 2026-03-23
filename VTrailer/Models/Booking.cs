using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;
using System;

namespace VTrailer.Models;

[Table("Bookings")]
public class Booking : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("TrailerId")]
    public int TrailerId { get; set; }

    [Column("TrailerName")]
    public string? TrailerName { get; set; }

    [Column("Username")]
    public string? Username { get; set; }

    [Column("CustomerName")]
    public string? CustomerName { get; set; }

    [Column("BookingDate")]
    public DateTime BookingDate { get; set; }

    [Column("TimeSlot")]
    public string? TimeSlot { get; set; }

    [Column("TotalPrice")]
    public decimal TotalPrice { get; set; }
  
    [JsonIgnore]
    public string DisplayDate => BookingDate.ToString("yyyy. MM. dd.");

    [JsonIgnore]
    public string DisplayPrice => $"{TotalPrice:N0} Ft";
}
