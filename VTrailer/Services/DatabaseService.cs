using SQLite;
using VTrailer.Models;

namespace VTrailer.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _db;

    public User? CurrentUser { get; private set; }

    public DatabaseService()
    {

        var dbPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "vtrailer_v3.db3");
        _db = new SQLiteAsyncConnection(dbPath);
    }

    public async Task InitializeDatabaseAsync()
    {
        await _db.CreateTableAsync<Trailer>();
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

        if (await _db.Table<Trailer>().CountAsync() == 0)
        {
            var trailers = new List<Trailer>
            {
                // Nyitott utánfutók
                new Trailer { LicensePlate = "XAE-342", Category = "Nyitott", BrandAndModel = "Brenderup 1205S", PayloadCapacityKg = 620, TotalWeightKg = 750, InnerLengthCm = 204, InnerWidthCm = 116, DailyRateFt = 4500, DepositFt = 20000, Status = "Elérhető", ImageUrl = "ms-appx:///Assets/brenderup1205S.png" },
                new Trailer { LicensePlate = "AA CZ-112", Category = "Nyitott", BrandAndModel = "Agados Handy 20", PayloadCapacityKg = 615, TotalWeightKg = 750, InnerLengthCm = 205, InnerWidthCm = 110, DailyRateFt = 4000, DepositFt = 20000, Status = "Kölcsönözve", ImageUrl = "ms-appx:///Assets/agadoshandy20.png" },
                new Trailer { LicensePlate = "WBB-987", Category = "Nyitott", BrandAndModel = "TPV Trailer EU 2", PayloadCapacityKg = 635, TotalWeightKg = 750, InnerLengthCm = 202, InnerWidthCm = 114, DailyRateFt = 3500, DepositFt = 15000, Status = "Elérhető", ImageUrl = "ms-appx:///Assets/tpvtrailerEU2.png" },

                // Ponyvás utánfutók
                new Trailer { LicensePlate = "KAA-522", Category = "Ponyvás", BrandAndModel = "Eduard 3116", PayloadCapacityKg = 1560, TotalWeightKg = 2000, InnerLengthCm = 310, InnerWidthCm = 160, DailyRateFt = 8500, DepositFt = 40000, Status = "Elérhető", ImageUrl = "ms-appx:///Assets/eduard3116.png" },
                new Trailer { LicensePlate = "AA TK-234", Category = "Ponyvás", BrandAndModel = "Blyss 2515", PayloadCapacityKg = 1150, TotalWeightKg = 1500, InnerLengthCm = 250, InnerWidthCm = 150, DailyRateFt = 7500, DepositFt = 35000, Status = "Kölcsönözve", ImageUrl = "ms-appx:///Assets/blyss2515.png" },

                // Autószállítók
                new Trailer { LicensePlate = "BCA-701", Category = "Autószállító", BrandAndModel = "Boro Atlas 4.5m", PayloadCapacityKg = 2050, TotalWeightKg = 2700, InnerLengthCm = 450, InnerWidthCm = 210, DailyRateFt = 12000, DepositFt = 100000, Status = "Elérhető", ImageUrl = "ms-appx:///Assets/boroatlas.png" },
                new Trailer { LicensePlate = "AB CQ-888", Category = "Autószállító", BrandAndModel = "Fitzel Euro 27-20/41T", PayloadCapacityKg = 2150, TotalWeightKg = 2700, InnerLengthCm = 415, InnerWidthCm = 200, DailyRateFt = 15000, DepositFt = 120000, Status = "Szervizben", ImageUrl = "ms-appx:///Assets/fitzeleuro.png" },

                // Speciális célú utánfutók
                new Trailer { LicensePlate = "BC HG-450", Category = "Motorszállító", BrandAndModel = "Stema MT 750", PayloadCapacityKg = 590, TotalWeightKg = 750, InnerLengthCm = 210, InnerWidthCm = 128, DailyRateFt = 5000, DepositFt = 25000, Status = "Elérhető", ImageUrl = "ms-appx:///Assets/stemamt.png" },
                new Trailer { LicensePlate = "PWM-403", Category = "Lószállító", BrandAndModel = "Böckmann Duo", PayloadCapacityKg = 1480, TotalWeightKg = 2400, InnerLengthCm = 328, InnerWidthCm = 165, DailyRateFt = 16000, DepositFt = 150000, Status = "Elérhető", ImageUrl = "ms-appx:///Assets/böckmannduo.png" },
                new Trailer { LicensePlate = "XHS-698", Category = "Hajószállító", BrandAndModel = "Pongratz PBA 1300", PayloadCapacityKg = 980, TotalWeightKg = 1300, InnerLengthCm = 550, InnerWidthCm = 190, DailyRateFt = 14000, DepositFt = 80000, Status = "Kölcsönözve", ImageUrl = "ms-appx:///Assets/pongratzpba.png" },
                new Trailer { LicensePlate = "AA MC-205", Category = "Gépszállító", BrandAndModel = "Ifor Williams GX105", PayloadCapacityKg = 2780, TotalWeightKg = 3500, InnerLengthCm = 303, InnerWidthCm = 152, DailyRateFt = 18000, DepositFt = 200000, Status = "Elérhető", ImageUrl = "ms-appx:///Assets/iforwilliams.png" },

                // Zárt és Billenős
                new Trailer { LicensePlate = "AA-XZ-999", Category = "Zárt dobozos", BrandAndModel = "Humbaur HK 132513", PayloadCapacityKg = 905, TotalWeightKg = 1300, InnerLengthCm = 251, InnerWidthCm = 132, DailyRateFt = 9000, DepositFt = 50000, Status = "Elérhető", ImageUrl = "ms-appx:///Assets/humbaurhk.png" },
                new Trailer { LicensePlate = "XTT-404", Category = "Billenős", BrandAndModel = "Humbaur HTK 2700", PayloadCapacityKg = 1900, TotalWeightKg = 2700, InnerLengthCm = 267, InnerWidthCm = 150, DailyRateFt = 14000, DepositFt = 150000, Status = "Elérhető", ImageUrl = "ms-appx:///Assets/humbaurhtk.png" }
            };
            await _db.InsertAllAsync(trailers);
        }
    }

    public async Task<List<Trailer>> GetTrailersAsync()
    {
        await _db.CreateTableAsync<Trailer>();
        return await _db.Table<Trailer>().ToListAsync();
    }

    public async Task<User?> GetUserAsync(string username, string password)
    {

        await _db.CreateTableAsync<User>();

        var user = await _db.Table<User>().Where(u => u.Username == username && u.Password == password).FirstOrDefaultAsync();
        if (user != null)
        {
            CurrentUser = user;
        }
        return user;
    }
    public async Task AddTrailerAsync(Trailer newTrailer)
    {
        await _db.InsertAsync(newTrailer);
    }
    public async Task AddUserAsync(User newUser)
    {
        await _db.InsertAsync(newUser);
    }
}
