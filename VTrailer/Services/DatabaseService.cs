using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VTrailer.Models;

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

    //FELHASZNÁLÓK

    public async Task<bool> LoginUserAsync(string email, string password)
    {
        try
        {
            // 1. Titkos Auth belépés
            var session = await _supabase.Auth.SignIn(email, password);

            if (session?.User != null)
            {
                // 2. Beállítjuk az e-mailt alapértelmezetten
                CurrentUser = new User { Email = email };

                // 3. LETÖLTJÜK A TELJES PROFILT A USERS TÁBLÁBÓL!
                var userProfile = await GetUserProfileAsync(email);
                if (userProfile != null)
                {
                    CurrentUser = userProfile; // Megkapja a Nevet, Telefont, mindent!
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

    //Regisztráció
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

    //Kijelentkezés
    public async Task LogoutAsync()
    {
        await _supabase.Auth.SignOut(); 
        CurrentUser = null;             
    }


    //UTÁNFUTÓK
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


    //FOGLALÁSOK
    public async Task AddBookingAsync(Booking newBooking)
    {
        await _supabase.InitializeAsync();
        await _supabase.From<Booking>().Insert(newBooking);
    }

    public async Task<List<Booking>> GetMyBookingsAsync(string email)
    {
        await _supabase.InitializeAsync();
        var response = await _supabase.From<Booking>()
                                      
                                      .Where(b => b.Email == email)
                                      .Get();
        return response.Models;
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
    // Jelszó módosítása 
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
}
