using Otakurin.Domain;
using Otakurin.Domain.Media;

namespace Otakurin.Service.Show;

public record APIShow(
    string Id,
    string CoverImageURL,
    string Title,
    string Summary,
    ShowType ShowType
);
