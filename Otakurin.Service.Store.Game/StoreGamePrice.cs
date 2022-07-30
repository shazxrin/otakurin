namespace Otakurin.Service.Store.Game;

public record StoreGamePrice(
    string URL,
    string Currency,
    double Price,
    bool IsOnSale,
    DateTime? SaleEnd
);
