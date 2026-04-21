using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace VTrailer.Models;

public class BookingDisplay
{
    public int BookingId { get; set; }

    // Utánfutó adatai
    public string TrailerBrandAndModel { get; set; } = "";
    public string TrailerLicensePlate { get; set; } = "";

    // Ügyfél adatai
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";

    // Időtartam
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Szép, olvasható dátum a UI-nak (pl. "Máj. 10 - Máj. 14")
    public string DateRangeFormatted => $"{StartDate.ToString("MMM. dd.")} - {EndDate.ToString("MMM. dd.")}";

    // Státusz (pl. "Folyamatban", "Következő", "Lezárva")
    public string Status { get; set; } = "Folyamatban";

    // KREATÍV EXTRÁNK: Automatikus szín a státusz alapján!
    public SolidColorBrush StatusColor
    {
        get
        {
            return Status.ToLower() switch
            {
                "folyamatban" => new SolidColorBrush(Colors.MediumSeaGreen), // Zöld
                "következő" => new SolidColorBrush(Colors.DodgerBlue),       // Kék
                "lezárva" => new SolidColorBrush(Colors.Gray),               // Szürke
                "lemondva" => new SolidColorBrush(Colors.IndianRed),         // Piros
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }
    }
}
