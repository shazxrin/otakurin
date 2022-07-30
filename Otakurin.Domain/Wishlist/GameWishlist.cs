namespace Otakurin.Domain.Wishlist;

#nullable disable

public class GameWishlist : Entity
{
    public Guid UserId { get; set; }
    
    public Guid GameId { get; set; }

    public string Platform { get; set; }
}
