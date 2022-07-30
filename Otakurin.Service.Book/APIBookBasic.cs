namespace Otakurin.Service.Book;

public record APIBookBasic(
    string Id,
    string CoverImageURL,
    string Title,
    List<string> Authors
);