using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Common;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Wishlist;

public class GetAllBookWishlistsQuery : PagedListRequest, IRequest<PagedListResult<GetAllBookWishlistsItemResult>>
{
    public Guid UserId { get; set; }
    
    public bool SortByRecentlyModified { get; set; } = false;
}

public class GetAllBookWishlistsValidator : AbstractValidator<GetAllBookWishlistsQuery>
{
    public GetAllBookWishlistsValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }    
}

public class GetAllBookWishlistsItemResult
{
    public Guid BookId { get; init; }
    
    public string Title { get; init; }
    
    public string CoverImageURL { get; init; }
}

public class GetAllBookWishlistsHandler : IRequestHandler<GetAllBookWishlistsQuery, PagedListResult<GetAllBookWishlistsItemResult>>
{
    private readonly DatabaseContext _databaseContext;

    public GetAllBookWishlistsHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<PagedListResult<GetAllBookWishlistsItemResult>> Handle(GetAllBookWishlistsQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetAllBookWishlistsValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var queryable = _databaseContext.BookWishlists
            .AsNoTracking()
            .Where(bw => bw.UserId.Equals(query.UserId));

        if (query.SortByRecentlyModified) queryable = queryable.OrderByDescending(bw => bw.LastModifiedOn);

        var joinQueryable = queryable.Join(
            _databaseContext.Books,
            bw => bw.BookId,
            b => b.Id,
            (bt, b) => new GetAllBookWishlistsItemResult 
            {
                BookId = b.Id,
                Title = b.Title,
                CoverImageURL = b.CoverImageURL
            }
        );
        
        var pagedList = await PagedListResult<GetAllBookWishlistsItemResult>.CreateAsync(
            joinQueryable,
            query.Page,
            query.PageSize,
            cancellationToken
        );

        return pagedList;
    }
}