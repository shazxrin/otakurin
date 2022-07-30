namespace Otakurin.Domain.Tracking;

#nullable disable

public enum MediaTrackingStatus { InProgress, Paused, Dropped, Planning, Completed }

public enum MediaTrackingFormat { Digital, Physical }

public enum MediaTrackingOwnership { Owned, Loan, Subscription }


public abstract class MediaTracking : Entity
{
    public Guid UserId { get; set; }

    public MediaTrackingFormat Format { get; set; }

    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}