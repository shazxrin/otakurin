using Otakurin.Domain.Wishlist;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Wishlist;

public class RemoveBookWishlistCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid BookId { get; set; }
}

public class RemoveBookWishlistValidator : AbstractValidator<RemoveBookWishlistCommand>
{
    public RemoveBookWishlistValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.BookId).NotEmpty();
    }
}

public class RemoveBookWishlistHandler : IRequestHandler<RemoveBookWishlistCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;

    public RemoveBookWishlistHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<Unit> Handle(RemoveBookWishlistCommand command, CancellationToken cancellationToken)
    {
        var validator = new RemoveBookWishlistValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        BookWishlist? bookWishlist = await _databaseContext.BookWishlists
            .Where(bw => bw.BookId.Equals(command.BookId) 
                         && bw.UserId.Equals(command.UserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (bookWishlist == null)
        {
            throw new NotFoundException("Book wishlist not found!");
        }

        _databaseContext.BookWishlists.Remove(bookWishlist);
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}