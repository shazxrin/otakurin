using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Common;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Wishlist;

public class GetAllGameWishlistsQuery : PagedListRequest, IRequest<PagedListResult<GetAllGameWishlistsItemResult>>
{
    public Guid UserId { get; set; }
    
    public bool SortByRecentlyModified { get; set; } = false;

    public bool SortByPlatform { get; set; } = false;
}

public class GetAllGameWishlistsValidator : AbstractValidator<GetAllGameWishlistsQuery>
{
    public GetAllGameWishlistsValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }
}

public class GetAllGameWishlistsItemResult
{
    public Guid GameId { get; set; }
    
    public string Title { get; set; }
    
    public string CoverImageURL { get; set; }
    
    public string Platform { get; set; }
}

public class GetAllGameWishlistsHandler : IRequestHandler<GetAllGameWishlistsQuery, PagedListResult<GetAllGameWishlistsItemResult>>
{
    private readonly DatabaseContext _databaseContext;

    public GetAllGameWishlistsHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<PagedListResult<GetAllGameWishlistsItemResult>> Handle(GetAllGameWishlistsQuery query,
        CancellationToken cancellationToken)
    {
        var validator = new GetAllGameWishlistsValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var queryable = _databaseContext.GameWishlists
            .AsNoTracking()
            .Where(gw => gw.UserId.Equals(query.UserId));

        if (query.SortByRecentlyModified) queryable = queryable.OrderByDescending(gw => gw.LastModifiedOn);
        if (query.SortByPlatform) queryable = queryable.OrderBy(gw => gw.Platform);

        var joinQueryable = queryable.Join(
            _databaseContext.Games,
            gw => gw.GameId,
            g => g.Id,
            (gw, g) => new GetAllGameWishlistsItemResult
            {
                GameId = g.Id,
                Title = g.Title,
                CoverImageURL = g.CoverImageURL,
                Platform = gw.Platform
            }
        );
        
        var pagedList = await PagedListResult<GetAllGameWishlistsItemResult>.CreateAsync(
            joinQueryable,
            query.Page,
            query.PageSize,
            cancellationToken
        );

        return pagedList;
    }
}