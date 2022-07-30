using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Otakurin.Service.Book;

internal class GoogleBooksAPIImageLinks
{
    [JsonPropertyName("thumbnail")] 
    public string? Thumbnail { get; set; }
}

internal class GoogleBooksAPIVolumeInfo
{
    [JsonPropertyName("title")] 
    public string? Title { get; set; }
    
    [JsonPropertyName("description")] 
    public string? Description { get; set; }

    [JsonPropertyName("authors")] 
    public List<string>? Authors { get; set; }

    [JsonPropertyName("imageLinks")] 
    public GoogleBooksAPIImageLinks? Images { get; set; }
}

internal class GoogleBooksAPISearchItem
{
    [JsonPropertyName("id")] 
    public string? Id { get; set; }

    [JsonPropertyName("volumeInfo")] 
    public GoogleBooksAPIVolumeInfo? VolumeInfo { get; set; }
}

internal class GoogleBooksAPIBookResult
{
    [JsonPropertyName("id")] 
    public string? Id { get; set; }
    
    [JsonPropertyName("volumeInfo")] 
    public GoogleBooksAPIVolumeInfo? VolumeInfo { get; set; }
}

internal class GoogleBooksAPISearchResult
{
    [JsonPropertyName("items")] 
    public List<GoogleBooksAPISearchItem>? Items { get; set; }
}

public class GoogleBooksAPIService : IBookService
{
    private readonly string _apiKey;

    private readonly HttpClient _httpClient;

    public GoogleBooksAPIService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    public async Task<List<APIBookBasic>> SearchBookByTitle(string title)
    {
        var query = title.Replace(' ', '+');

        var results = await _httpClient.GetFromJsonAsync<GoogleBooksAPISearchResult>(
            $"https://www.googleapis.com/books/v1/volumes?q={query}&key={_apiKey}"
        );

        if (results == null)
        {
            return new List<APIBookBasic>();
        }
        
        return results.Items?
            .Select(item => new APIBookBasic(
                item.Id ?? "",
                item.VolumeInfo?.Images?.Thumbnail ?? "",
                item.VolumeInfo?.Title ?? "",
                item.VolumeInfo?.Authors ?? new List<string>()))
            .ToList() ?? new List<APIBookBasic>();
    }

    public async Task<APIBook?> GetBookById(string id)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<GoogleBooksAPIBookResult>(
                $"https://www.googleapis.com/books/v1/volumes/{id}?key={_apiKey}"
            );

            if (result == null)
            {
                return null;
            }

            return new APIBook(
                result.Id ?? "",
                result.VolumeInfo?.Images?.Thumbnail ?? "",
                result.VolumeInfo?.Title ?? "",
                result.VolumeInfo?.Description ?? "",
                result.VolumeInfo?.Authors ?? new List<string>()
            );
        }
        catch
        {
            return null;
        }
    }
}