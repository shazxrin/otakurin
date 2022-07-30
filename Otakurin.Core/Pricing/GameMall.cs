using Otakurin.Domain;
using Otakurin.Domain.Pricing;
using Otakurin.Service.Store.Game;
using Otakurin.Service.Store.Game.Switch;

namespace Otakurin.Core.Pricing;

public class GameMall : IGameMall
{
    private readonly Dictionary<GameStoreType, IGameStore> _gameStores = new ();

    public GameMall()
    {
        _gameStores[GameStoreType.Switch] = new SwitchGameStore();
    }

    public IGameStore GetGameStore(GameStoreType gameStoreType) => _gameStores[gameStoreType];
}