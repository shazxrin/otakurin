namespace Otakurin.Service.Book;

public record APIBook(
    string Id,
    string CoverImageURL,
    string Title,
    string Summary,
    List<string> Authors
);
