using AutoMapper;
using Otakurin.Domain.Wishlist;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Wishlist;

public class AddGameWishlistCommand : IRequest<Unit>
{
    public Guid UserId { get; set; } = Guid.Empty;
    
    public Guid GameId { get; set; } = Guid.Empty;

    public string Platform { get; set; } = string.Empty;
}

public class AddGameWishlistValidator : AbstractValidator<AddGameWishlistCommand>
{
    public AddGameWishlistValidator()
    {
        RuleFor(c => c.UserId).NotEqual(Guid.Empty);
        RuleFor(c => c.Platform).NotEmpty();
    }
}

public class AddGameWishlistMappings : Profile
{
    public AddGameWishlistMappings()
    {
        CreateMap<AddGameWishlistCommand, GameWishlist>();
    }
}

public class AddGameWishlistHandler : IRequestHandler<AddGameWishlistCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public AddGameWishlistHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<Unit> Handle(AddGameWishlistCommand command, CancellationToken cancellationToken)
    {
        var validator = new AddGameWishlistValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
        // Verify user.
        bool isUserExists = await _databaseContext.Users
            .AsNoTracking()
            .Where(u => u.Id.Equals(command.UserId))
            .AnyAsync(cancellationToken);

        if (!isUserExists)
        {
            throw new NotFoundException("User not found!");
        }
        
        // Verify if tracked game already exist.
        bool isGameWishlistExists = await _databaseContext.GameWishlists
            .AsNoTracking()
            .Where(gw => gw.GameId.Equals(command.GameId) 
                         && gw.UserId.Equals(command.UserId)
                         && gw.Platform.Equals(command.Platform))
            .AnyAsync(cancellationToken);

        if (isGameWishlistExists)
        {
            throw new ExistsException("Game wishlist already exists!");
        }
        
        // Verify game id.
        bool isGameExists = await _databaseContext.Games
            .AsNoTracking()
            .Where(g => g.Id.Equals(command.GameId))
            .AnyAsync(cancellationToken);

        if (!isGameExists)
        {
            throw new NotFoundException("Game not found!");
        }

        var gameWishlist = _mapper.Map<AddGameWishlistCommand, GameWishlist>(command);
        _databaseContext.GameWishlists.Add(gameWishlist);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}