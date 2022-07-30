using Otakurin.Service.Store.Game.Switch.NoE;

namespace Otakurin.Service.Store.Game.Switch;

public class Regions
{
    public const string UK = "GB";
    public const string AU = "AU";
    public const string NZ = "NZ";
    public static readonly List<string> All = new() { UK, AU, NZ };
}

public class SwitchGameStore : IGameStore
{
    private readonly IEShopRegionGameStore _noeEShopRegionGameStore = new EShopNoEGameStore();
    private readonly Dictionary<string, IEShopRegionGameStore> _regionEShopGameStores = new();

    public SwitchGameStore()
    {
        _regionEShopGameStores.Add(Regions.UK, _noeEShopRegionGameStore);
        _regionEShopGameStores.Add(Regions.AU, _noeEShopRegionGameStore);
        _regionEShopGameStores.Add(Regions.NZ, _noeEShopRegionGameStore);
    }

    public List<string> GetSupportedRegions()
    {
        return Regions.All;
    }

    public async Task<string?> SearchGameStoreId(string region, string gameTitle)
    {
        if (!_regionEShopGameStores.ContainsKey(region))
        {
            return null;
        }

        return await _regionEShopGameStores[region].SearchGameStoreId(gameTitle);
    }

    public async Task<StoreGamePrice?> GetGamePrice(string region, string gameStoreId)
    {
        if (!_regionEShopGameStores.ContainsKey(region))
        {
            return null;
        }

        return await _regionEShopGameStores[region].GetGamePrice(region, gameStoreId);
    }
}