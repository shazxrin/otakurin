using IGDBLib = IGDB;

namespace Otakurin.Service.Game;

public class IGDBAPIService : IGameService
{
    private readonly IGDBLib.IGDBClient _client;

    public IGDBAPIService(string clientId, string clientSecret)
    {
        _client = new IGDBLib.IGDBClient(clientId, clientSecret);
    }

    public async Task<List<APIGameBasic>> SearchGameByTitle(string title)
    {
        var result = await _client.QueryAsync<IGDBLib.Models.Game>(
            IGDBLib.IGDBClient.Endpoints.Games,
            $"search \"{title.ToLower()}\"; fields name,cover.*,platforms.*;"
        );

        List<APIGameBasic> list = new();
        
        foreach (var game in result)
        {
            if (game.Id != null && game.Platforms != null)
            {
                var platforms = new List<string>();
                foreach (var platform in game.Platforms.Values)
                {
                    platforms.Add(platform.Abbreviation ?? platform.Name);
                }
                
                var coverURL = game.Cover != null ? "https:" + game.Cover.Value.Url : "";
                if (coverURL.Length > 0)
                {
                    coverURL = coverURL.Replace("t_thumb", "t_cover_big");
                }

                list.Add(new(
                    game.Id.Value,
                    coverURL,
                    game.Name,
                    platforms
                ));
            }
        }

        return list;
    }

    public async Task<APIGame?> GetGameById(long id)
    {
        // TODO: Complete query.
        var result = await _client.QueryAsync<IGDBLib.Models.Game>(
            IGDBLib.IGDBClient.Endpoints.Games,
            $"fields name,platforms.*,summary,cover.*,involved_companies.company.*,screenshots.url; where id = ({id});"
        );

        if (result.Length > 0)
        {
            var game = result[0];
            if (game.Id != null && game.Platforms != null)
            {
                var platforms = new List<string>();
                foreach (var platform in game.Platforms.Values)
                {
                    platforms.Add(platform.Abbreviation ?? platform.Name);
                }

                var coverURL = game.Cover != null ? "https:" + game.Cover.Value.Url : "";
                if (coverURL.Length > 0)
                {
                    coverURL = coverURL.Replace("t_thumb", "t_cover_big");
                }
                
                var companies = new List<string>();
                if (game.InvolvedCompanies != null)
                {
                    foreach (var company in game.InvolvedCompanies.Values)
                    {
                        companies.Add(company.Company.Value.Name);
                    }
                }

                var screenshotsUrls = new List<string>();
                if (game.Screenshots != null)
                {
                    foreach (var screenshot in game.Screenshots.Values)
                    {
                        if (screenshot == null)
                        {
                            continue;
                        }
                        
                        if (string.IsNullOrEmpty(screenshot.Url))
                        {
                            continue;
                        }
                        
                        var screenshotUrl = $"https:{screenshot.Url}";
                        if (screenshotUrl.Length > 0)
                        {
                            screenshotUrl = screenshotUrl.Replace("t_thumb", "t_original");
                        }
                        
                        screenshotsUrls.Add(screenshotUrl);
                    }
                }
                
                return new APIGame(
                    game.Id.Value,
                    coverURL,
                    game.Name,
                    game.Summary,
                    screenshotsUrls,
                    platforms,
                    companies
                );
            }
        }

        return null; 
    }
}