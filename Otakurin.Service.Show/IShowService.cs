using Otakurin.Domain;

namespace Otakurin.Service.Show;

public interface IShowService
{
    Task<List<APIShowBasic>> SearchShowByTitle(string title);

    Task<APIShow?> GetShowById(string id);
}