using Otakurin.Domain.Pricing;
using Otakurin.Service.Store.Game;

namespace Otakurin.Core.Pricing;

public interface IGameMall
{
    IGameStore GetGameStore(GameStoreType gameStoreType);
}
