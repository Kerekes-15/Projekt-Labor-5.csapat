using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VTrailer.Models;
using VTrailer.Services;

namespace VTrailer.Presentation;

public partial class TrailerViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    public ObservableCollection<Trailer> Trailers { get; } = new ObservableCollection<Trailer>();

    public TrailerViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;

        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        var data = await _databaseService.GetTrailersAsync();
        Trailers.Clear();
        foreach (var item in data)
        {
            Trailers.Add(item);
        }
    }
}
