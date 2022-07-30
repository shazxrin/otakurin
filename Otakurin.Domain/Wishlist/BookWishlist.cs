namespace Otakurin.Domain.Wishlist;

#nullable disable

public class BookWishlist : Entity
{
    public Guid UserId { get; set; }
    
    public Guid BookId { get; set; }
}
