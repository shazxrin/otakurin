namespace Otakurin.Service.Game;

public record APIGameBasic(
    long Id,
    string CoverImageURL,
    string Title,
    List<string> Platforms
);