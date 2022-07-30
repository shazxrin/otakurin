using Otakurin.Domain.Tracking;

namespace Otakurin.Domain.User;

#nullable disable

public enum ActivityMediaType { Game, Show, Book }
public enum ActivityAction { AddTracking, UpdateTracking, RemoveTracking, AddWishlist, RemoveWishlist }

public class UserActivity : Entity
{
    public Guid UserId { get; set; }
    
    public Guid MediaId { get; set; }
    
    public string MediaTitle { get; set; }
    
    public string MediaCoverImageURL { get; set; }

    public int NoOf { get; set; }
    
    public MediaTrackingStatus? Status { get; set; } 
    
    public ActivityMediaType MediaType { get; set; }
    
    public ActivityAction Action { get; set; }
}