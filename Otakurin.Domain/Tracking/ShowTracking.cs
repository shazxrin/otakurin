using Otakurin.Domain.Media;

namespace Otakurin.Domain.Tracking;

#nullable disable

public class ShowTracking : MediaTracking
{
    public Guid ShowId { get; set; }
    
    public int EpisodesWatched { get; set; }
}