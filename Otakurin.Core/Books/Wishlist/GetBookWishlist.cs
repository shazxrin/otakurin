using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Wishlist;

public class GetBookWishlistQuery : IRequest<bool>
{
    public Guid UserId { get; set; }
    
    public Guid BookId { get; set; }
}

public class GetBookWishlistValidator : AbstractValidator<GetBookWishlistQuery>
{
    public GetBookWishlistValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.BookId).NotEmpty();
    }    
}

public class GetBookWishlistHandler : IRequestHandler<GetBookWishlistQuery, bool>
{
    private readonly DatabaseContext _databaseContext;

    public GetBookWishlistHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<bool> Handle(GetBookWishlistQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetBookWishlistValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var hasBookWishlist = await _databaseContext.BookWishlists
            .AsNoTracking()
            .Where(bw => bw.UserId.Equals(query.UserId) && bw.BookId.Equals(query.BookId))
            .AnyAsync(cancellationToken);

        return hasBookWishlist;
    }
}
