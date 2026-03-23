using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using VTrailer.Models;

namespace VTrailer.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _db;
    private readonly Supabase.Client _supabase;

    public User? CurrentUser { get; private set; }

    public DatabaseService()
    {
        //HELYI ADATBÁZIS (SQLite - Csak a bejelentkezéshez)
        var dbPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "vtrailer_v3.db3");
        _db = new SQLiteAsyncConnection(dbPath);

        //Supabase adatbázis
        var url = "https://nadkndxtehghzcviactm.supabase.co";
        var key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5hZGtuZHh0ZWhnaHpjdmlhY3RtIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzQyMTIyNDcsImV4cCI6MjA4OTc4ODI0N30.aiafjCqQcNUId2OV018kdaezBotw5x_PYd1Lir-NF9Y";

        var options = new Supabase.SupabaseOptions { AutoConnectRealtime = false };
        _supabase = new Supabase.Client(url, key, options);
    }

    public async Task InitializeDatabaseAsync()
    {
        await _db.CreateTableAsync<User>();

        if (await _db.Table<User>().CountAsync() == 0)
        {
            var users = new List<User>
            {
                new User { Username = "kovacsj", Password = "kjanos2000", Role = "Adminisztrátor", FullName = "Kovács János", Email = "kovacs.janos@vtrailer.hu", PhoneNumber = "+36 30 404 4986" },
                new User { Username = "nagyg", Password = "gabor1997", Role = "Adminisztrátor", FullName = "Nagy Gábor", Email = "nagy.gabor@vtrailer.hu", PhoneNumber = "+36 20 987 6543" },
                new User { Username = "szabojozsef", Password = "vtelep123", Role = "Alkalmazott", FullName = "Szabó József", Email = "jozsef.telep@vtrailer.hu", PhoneNumber = "+36 20 888 6510" },
                new User { Username = "kovacsanna", Password = "romanckedvelo", Role = "Vendég", FullName = "Kovács Anna", Email = "kovacsanna21@gmail.com", PhoneNumber = "+36 70 931 1546" },
                new User { Username = "takacsz", Password = "zoltan6991", Role = "Vendég", FullName = "Takács Zoltán", Email = "takacszoli@hotmail.com", PhoneNumber = "+36 30 987 6543" },
                new User { Username = "vargae", Password = "kismacska34", Role = "Vendég", FullName = "Varga Eszter", Email = "v.eszter92@gmail.com", PhoneNumber = "+36 20 555 7890" },
            };
            await _db.InsertAllAsync(users);
        }
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

    //FOGLALÁSOK

    // 1. Új foglalás elmentése a felhőbe
    public async Task AddBookingAsync(Booking newBooking)
    {
        await _supabase.InitializeAsync();
        await _supabase.From<Booking>().Insert(newBooking);
    }

    // 2. Az utánfutó státuszának átírása (pl. "Elérhető"-ről "Kölcsönözve"-re)
    public async Task UpdateTrailerStatusAsync(int trailerId, string newStatus)
    {
        await _supabase.InitializeAsync();

        // Megkeressük az utánfutót az ID alapján, és átírjuk a státuszát
        await _supabase.From<Trailer>()
            .Where(t => t.Id == trailerId)
            .Set(t => t.Status!, newStatus)
            .Update();
    }

    // 3. Egy adott felhasználó saját foglalásainak lekérdezése
    public async Task<List<Booking>> GetMyBookingsAsync(string username)
    {
        await _supabase.InitializeAsync();

        // Csak azokat kérjük le, ahol a Username megegyezik a keresettel
        var response = await _supabase.From<Booking>()
                                      .Where(b => b.Username == username)
                                      .Get();

        return response.Models;
    }

    //FELHASZNÁLÓK
    public async Task<User?> GetUserAsync(string username, string password)
    {
        var user = await _db.Table<User>().Where(u => u.Username == username && u.Password == password).FirstOrDefaultAsync();
        if (user != null)
        {
            CurrentUser = user;
        }
        return user;
    }

    public async Task AddUserAsync(User newUser)
    {
        await _db.InsertAsync(newUser);
    }
}
