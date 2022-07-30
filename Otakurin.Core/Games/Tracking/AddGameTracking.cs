using AutoMapper;
using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Tracking;

public class AddGameTrackingCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid GameId { get; set; }
    
    public float HoursPlayed { get; set; }
    
    public string Platform { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}

public class AddGameTrackingValidator : AbstractValidator<AddGameTrackingCommand>
{
    public AddGameTrackingValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.GameId).NotEmpty();
        RuleFor(c => c.Platform).NotEmpty();
    }
}

public class AddGameTrackingMappings : Profile
{
    public AddGameTrackingMappings()
    {
        CreateMap<AddGameTrackingCommand, GameTracking>();
    }
}

public class AddGameTrackingHandler : IRequestHandler<AddGameTrackingCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public AddGameTrackingHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(AddGameTrackingCommand command, CancellationToken cancellationToken)
    {
        var validator = new AddGameTrackingValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
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
        bool isGameTrackingExists = await _databaseContext.GameTrackings
            .AsNoTracking()
            .Where(gt => gt.GameId.Equals(command.GameId) 
                         && gt.UserId.Equals(command.UserId)
                         && gt.Platform.Equals(command.Platform))
            .AnyAsync(cancellationToken);
        if (isGameTrackingExists)
        {
            throw new ExistsException("Game tracking already exists!");
        }
        
        var gameTracking = _mapper.Map<AddGameTrackingCommand, GameTracking>(command);
        _databaseContext.GameTrackings.Add(gameTracking);
        
        // Verify game id.
        var game = await _databaseContext.Games
            .AsNoTracking()
            .Where(g => g.Id.Equals(command.GameId))
            .FirstOrDefaultAsync(cancellationToken);
        if (game == null)
        {
            throw new NotFoundException("Game not found!");
        }

        var userActivity = new UserActivity
        {
            UserId = gameTracking.UserId,
            Status = gameTracking.Status,
            NoOf = gameTracking.HoursPlayed,
            MediaId = game.Id,
            MediaTitle = game.Title,
            MediaCoverImageURL = game.CoverImageURL,
            MediaType = ActivityMediaType.Game,
            Action = ActivityAction.AddTracking
        };
        _databaseContext.Activities.Add(userActivity);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}
