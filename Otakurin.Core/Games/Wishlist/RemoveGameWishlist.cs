using Otakurin.Domain.Wishlist;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Wishlist;

public class RemoveGameWishlistCommand : IRequest<Unit>
{
    public Guid UserId { get; set; } = Guid.Empty;

    public Guid GameId { get; set; } = Guid.Empty;
    
    public string Platform { get; set; } = string.Empty; 
}

public class RemoveGameWishlistValidator : AbstractValidator<RemoveGameWishlistCommand>
{
    public RemoveGameWishlistValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Platform).NotEmpty();
    }
}

public class RemoveGameWishlistHandler : IRequestHandler<RemoveGameWishlistCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;

    public RemoveGameWishlistHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<Unit> Handle(RemoveGameWishlistCommand command, CancellationToken cancellationToken)
    {
        var validator = new RemoveGameWishlistValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }        
        
        GameWishlist? gameWishlist = await _databaseContext.GameWishlists
            .Where(gw => gw.GameId.Equals(command.GameId)
                         && gw.UserId.Equals(command.UserId)
                         && gw.Platform.Equals(command.Platform))
            .FirstOrDefaultAsync(cancellationToken);

        if (gameWishlist == null)
        {
            throw new NotFoundException("Game wishlist not found!");
        }

        _databaseContext.GameWishlists.Remove(gameWishlist);
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}