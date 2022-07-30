using Otakurin.Domain;
using Otakurin.Domain.Media;

namespace Otakurin.Service.Show;

public record APIShowBasic(
    string Id,
    string CoverImageURL,
    string Title,
    ShowType ShowType
);