namespace Otakurin.Service.Game;

public interface IGameService
{
    Task<List<APIGameBasic>> SearchGameByTitle(string title);

    Task<APIGame?> GetGameById(long id);
}
