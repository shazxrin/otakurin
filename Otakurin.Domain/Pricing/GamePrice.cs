namespace Otakurin.Domain.Pricing;

#nullable disable

public class GamePrice : Entity
{
    public Guid GameId { get; set; }
    
    public GameStoreType GameStoreType { get; set; }
    
    public string Region { get; set; }
    
    public string URL { get; set; }
    
    public string Currency { get; set; }
    
    public double Price { get; set; }
    
    public bool IsOnSale { get; set; }
    
    public DateTime? SaleEnd { get; set; }
}