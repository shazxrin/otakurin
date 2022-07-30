namespace Otakurin.Service.Store.Game;

public interface IGameStore
{
    Task<string?> SearchGameStoreId(string region, string gameTitle);
    
    Task<StoreGamePrice?> GetGamePrice(string region, string gameStoreId);

    List<string> GetSupportedRegions();
}
