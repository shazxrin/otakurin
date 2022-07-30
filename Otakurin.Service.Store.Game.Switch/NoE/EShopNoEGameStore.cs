using System.Net.Http.Json;
using System.Web;
using Quickenshtein;

namespace Otakurin.Service.Store.Game.Switch.NoE;

public class EShopNoEGameStore : IEShopRegionGameStore
{
    private const string NoESearchAPIURL = "https://search.nintendo-europe.com/en/select";
    private const string NoEPriceAPIURL = "https://api.ec.nintendo.com/v1/price";
    
    private readonly HttpClient _httpClient;

    public EShopNoEGameStore()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string?> SearchGameStoreId(string gameTitle)
    {
        var searchResults = new List<NintendoSearchAPIDoc>();

        const int startOffset = 10;
        int start = -10;
        for (int page = 1; page <= 2; page++)
        {
            var uriBuilder = new UriBuilder(NoESearchAPIURL);
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["fq"] = "type:GAME AND system_names_txt:\"Switch\"";
            query["q"] = gameTitle;
            query["start"] = (start += startOffset).ToString();
            uriBuilder.Query = query.ToString();

            var responseBody
                = await _httpClient.GetFromJsonAsync<NintendoSearchAPIBody>(uriBuilder.ToString());
            if (responseBody != null)
            {
                if (responseBody.Response?.Docs != null)
                {
                    searchResults.AddRange(responseBody.Response.Docs);
                }
            }
        }

        var normalizedGameTitle = gameTitle.ToLower().Replace(" ", "");

        int lowestDist = normalizedGameTitle.Length;
        string? nintendoId = null;
        foreach (var searchResult in searchResults)
        {
            int dist = Levenshtein.GetDistance(
                searchResult.Title?.ToLower().Replace(" ", ""),
                normalizedGameTitle
            );
            
            if (dist < lowestDist)
            {
                lowestDist = dist;
                nintendoId = searchResult.NsuidTxt?.ElementAtOrDefault(0);
            }
        }

        return nintendoId;
    }
    
    public async Task<StoreGamePrice?> GetGamePrice(string region, string gameStoreId)
    {
        var uriBuilder = new UriBuilder(NoEPriceAPIURL);
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["country"] = region;
        query["ids"] = gameStoreId;
        query["lang"] = "en";
        uriBuilder.Query = query.ToString();

        var responseBody
            = await _httpClient.GetFromJsonAsync<NintendoPriceAPIResponse>(uriBuilder.ToString());
        if (responseBody == null) return null;

        NintendoPriceAPIPrice? price = responseBody.Prices?.ElementAtOrDefault(0);
        if (price == null) return null;

        string currency = price.RegularPrice?.Currency ?? "???";
        double currentPrice = price.RegularPrice?.Amount ?? 0;
        bool isOnSale = false;
        DateTime? saleEnd = null;

        if (price.DiscountPrice != null)
        {
            isOnSale = true;
            currentPrice = price.DiscountPrice.Amount;
            saleEnd = price.DiscountPrice.EndDatetime;
        }

        return new StoreGamePrice(
            $"https://ec.nintendo.com/{region}/en/titles/{gameStoreId}",
            currency,
            currentPrice,
            isOnSale,
            saleEnd
        );
    }
}
