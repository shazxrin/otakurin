using Otakurin.Service.Game;

namespace Otakurin.Application.Extensions;

public static class ExternalAPIServiceExtentions
{
    public static void AddExternalAPIServices(this IServiceCollection services, IConfiguration configuration)
    {
        var clientId = configuration["IGDB:ClientId"];
        var clientSecret = configuration["IGDB:ClientSecret"];

        if (clientId == null || clientSecret == null)
        {
            throw new ApplicationException("IGDB ID and secret may have not been configured!");
        }
        
        services.AddSingleton<IGameService>(new IGDBAPIService(clientId, clientSecret));
    }
}