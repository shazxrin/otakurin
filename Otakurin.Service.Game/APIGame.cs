namespace Otakurin.Service.Game;

public record APIGame(
    long Id,
    string CoverImageURL,
    string Title,
    string Summary,
    List<string> Platforms,
    List<string> Companies
);
