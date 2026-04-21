using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VTrailer.Models;
using System.Security.Cryptography;

namespace VTrailer.Services;

public class DatabaseService
{
    private readonly Supabase.Client _supabase;

    public static User? CurrentUser { get; private set; }

    public DatabaseService()
    {
        var url = "https://nadkndxtehghzcviactm.supabase.co/";
        var key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5hZGtuZHh0ZWhnaHpjdmlhY3RtIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzQyMTIyNDcsImV4cCI6MjA4OTc4ODI0N30.aiafjCqQcNUId2OV018kdaezBotw5x_PYd1Lir-NF9Y";

        var options = new Supabase.SupabaseOptions { AutoConnectRealtime = false };
        _supabase = new Supabase.Client(url, key, options);
    }

    public async Task<bool> LoginUserAsync(string email, string password)
    {
        try
        {
            var session = await _supabase.Auth.SignIn(email, password);

            if (session?.User != null)
            {
                CurrentUser = new User { Email = email };

                var userProfile = await GetUserProfileAsync(email);
                if (userProfile != null)
                {
                    CurrentUser = userProfile;
                }

                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bejelentkezési hiba: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RegisterUserAsync(string email, string password, string fullName, string phoneNumber)
    {
        try
        {
            var session = await _supabase.Auth.SignUp(email, password);

            if (session?.User != null)
            {
                var newUserProfile = new Models.User
                {
                    Email = email,
                    FullName = fullName,
                    PhoneNumber = phoneNumber,
                    Role = "Vendég"
                };

                await _supabase.From<Models.User>().Insert(newUserProfile);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Regisztrációs hiba: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _supabase.Auth.SignOut();
        CurrentUser = null;
    }

    public async Task<List<Trailer>> GetTrailersAsync()
    {
        await _supabase.InitializeAsync();
        var response = await _supabase.From<Trailer>().Get();
        return response.Models;
    }

    public async Task AddTrailerAsync(Trailer newTrailer)
    {
        await _supabase.InitializeAsync();
        await _supabase.From<Trailer>().Insert(newTrailer);
    }

    public async Task UpdateTrailerStatusAsync(int trailerId, string newStatus)
    {
        await _supabase.InitializeAsync();
        await _supabase.From<Trailer>()
            .Where(t => t.Id == trailerId)
            .Set(t => t.Status!, newStatus)
            .Update();
    }

    public async Task UpdateTrailerAsync(Trailer updatedTrailer)
    {
        try
        {
            await _supabase.InitializeAsync();
            await _supabase.From<Trailer>().Update(updatedTrailer);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Hiba az utánfutó frissítésekor: {ex.Message}");
        }
    }

    public async Task AddBookingAsync(Booking newBooking)
    {
        await _supabase.InitializeAsync();
        await _supabase.From<Booking>().Insert(newBooking);
    }

    public async Task AddBookingsAsync(IEnumerable<Booking> newBookings)
    {
        await _supabase.InitializeAsync();

        foreach (var booking in newBookings)
        {
            await _supabase.From<Booking>().Insert(booking);
        }
    }

    // 1. Saját foglalások lekérése e-mail alapján
    public async Task<List<Booking>> GetMyBookingsAsync(string userEmail)
    {
        await _supabase.InitializeAsync();
        var response = await _supabase
            .From<Booking>()
            .Where(b => b.Email == userEmail) 
            .Get();
        return response.Models;
    }

    // 2. Összes foglalás (AllBookings) név-szinkronizálása e-mail alapján
    public async Task<List<Booking>> GetAllBookingsAsync()
    {
        await _supabase.InitializeAsync();

        var bookings = (await _supabase.From<Booking>().Get()).Models;
        var users = (await _supabase.From<User>().Get()).Models;

        foreach (var booking in bookings)
        {
            if (!string.IsNullOrWhiteSpace(booking.Email))
            {
                // Email alapján párosítjuk a nevet a Users táblából
                var matchedUser = users.FirstOrDefault(u => u.Email == booking.Email);
                if (matchedUser != null)
                {
                    booking.CustomerName = matchedUser.FullName;
                }
            }
        }
        return bookings;
    }

    public async Task<User?> GetUserProfileAsync(string email)
    {
        try
        {
            var response = await _supabase.From<User>()
                                          .Where(u => u.Email == email)
                                          .Get();

            return response.Models.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> SaveUserProfileAsync(User userProfile)
    {
        try
        {
            var response = await _supabase.From<User>().Upsert(userProfile);
            return response.Models.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Profil mentési hiba: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(string newPassword)
    {
        try
        {
            var attrs = new Supabase.Gotrue.UserAttributes { Password = newPassword };
            var response = await _supabase.Auth.Update(attrs);
            return response != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Jelszó módosítási hiba: {ex.Message}");
            return false;
        }
    }

    public async Task LogActionAsync(string action, string tableName, string details)
    {
        try
        {
            var entry = new AuditEntry
            {
                Action = action,
                TargetTable = tableName,
                UserEmail = CurrentUser?.Email ?? "Rendszer",
                Details = details
            };
            await _supabase.From<AuditEntry>().Insert(entry);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Log hiba: {ex.Message}"); }
    }

    public async Task<List<AuditEntry>> GetAuditLogsAsync()
    {
        var response = await _supabase.From<AuditEntry>().Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending).Get();
        return response.Models;
    }

    public async Task<bool> DeleteBookingAsync(int bookingId, string trailerName)
    {
        try
        {
            await _supabase.InitializeAsync();

            var response = await _supabase.From<Booking>()
                .Where(b => b.Id == bookingId)
                .Get();

            var bookingToDelete = response.Models.FirstOrDefault();

            if (bookingToDelete != null)
            {
                await _supabase.From<Trailer>()
                    .Where(t => t.Id == bookingToDelete.TrailerId)
                    .Set(t => t.Status!, "Elérhető")
                    .Update();
            }

            await _supabase.From<Booking>()
                .Where(b => b.Id == bookingId)
                .Delete();

            string userWhoDeleted = CurrentUser?.FullName ?? "Ismeretlen";
            await LogActionAsync("DELETE", "Booking",
                $"Lemondás: {userWhoDeleted} törölte a foglalását. Utánfutó: {trailerName} felszabadítva.");

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Hiba a törlésnél: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ReturnTrailerAsync(Booking booking)
    {
        try
        {
            await _supabase.From<Trailer>()
                .Where(t => t.Id == booking.TrailerId)
                .Set(t => t.Status!, "Elérhető")
                .Update();

            await _supabase.From<Booking>()
                .Where(b => b.Id == booking.Id)
                .Delete();

            string userName = booking.CustomerName ?? "Ismeretlen";
            string trailerName = booking.TrailerName ?? "Ismeretlen utánfutó";

            await LogActionAsync("RETURN", "Booking", $"Utánfutó visszavéve. Típus: {trailerName}, Ügyfél: {userName}");

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            return false;
        }
    }
    public async Task<string> UploadTrailerImageAsync(Windows.Storage.StorageFile file)
    {
        try
        {
            await _supabase.InitializeAsync();

            using var stream = await file.OpenStreamForReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            string extension = Path.GetExtension(file.Name).ToLowerInvariant();
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";

            await _supabase.Storage.From("images").Upload(fileBytes, uniqueFileName, new Supabase.Storage.FileOptions { Upsert = true });

            return _supabase.Storage.From("images").GetPublicUrl(uniqueFileName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Hiba a képfeltöltéskor: {ex.Message}");
            throw; 
        }
    }
    public async Task DeleteTrailerAsync(int id)
    {
        try
        {
            await _supabase.InitializeAsync();
            await _supabase.From<Trailer>()
                           .Where(t => t.Id == id)
                           .Delete();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Hiba az utánfutó törlésekor: {ex.Message}");
        }
    }
    public async Task<List<Booking>> GetBookingsForTrailerAsync(int trailerId)
    {
        await _supabase.InitializeAsync();

        var response = await _supabase
            .From<Booking>()
            .Where(b => b.TrailerId == trailerId)
            .Get();

        return response.Models;
    }
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        await _supabase.InitializeAsync();
        var response = await _supabase.From<User>().Where(u => u.Email == email).Get();
        return response.Models.FirstOrDefault();
    }
}   
