using Otakurin.Domain;
using Otakurin.Domain.Media;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace Otakurin.Service.Show;

public class TMDBAPIService : IShowService
{
    private readonly TMDbClient _client;
    
    private readonly string imageURL = "https://www.themoviedb.org/t/p/w300_and_h450_bestv2";

    public TMDBAPIService(string apiKey)
    {
        _client = new TMDbClient(apiKey);
    }

    public async Task<List<APIShowBasic>> SearchShowByTitle(string title)
    {
        List<APIShowBasic> list = new();
        var searchContainer = await _client.SearchMultiAsync(title);
        var showList = searchContainer.Results
            .Where(sb => sb.MediaType != MediaType.Person)
            .ToList();

        foreach (var show in showList)
        {
            if (show.MediaType == MediaType.Movie)
            {
                SearchMovie movie = show as SearchMovie;
                list.Add(new(
                    "m_" + movie.Id,
                    imageURL + movie.PosterPath,
                    movie.Title,
                    ShowType.Movie));
            }
            else
            {
                SearchTv series = show as SearchTv;
                list.Add(new(
                    "s_" + series.Id,
                    imageURL + series.PosterPath,
                    series.Name,
                    ShowType.Series));
            }
        }
        return list;
    }

    public async Task<APIShow?> GetShowById(string id)
    {
        int intId = Convert.ToInt32(id.Split("_").Last());

        if (id.Split("_").First() == "m")
        {
            Movie movie = await _client.GetMovieAsync(intId);
            return new(
                "m_" + movie.Id,
                imageURL + movie.PosterPath,
                movie.Title,
                movie.Overview,
                ShowType.Movie);

        }
        else if (id.Split("_").First() == "s")
        {
            TvShow series = await _client.GetTvShowAsync(intId);
            return new(
                "s_" + series.Id,
                imageURL + series.PosterPath,
                series.Name,
                series.Overview,
                ShowType.Series);
        }
        return null;
    }
}