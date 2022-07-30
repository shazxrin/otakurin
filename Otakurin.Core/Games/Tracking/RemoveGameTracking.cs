using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Tracking;

public class RemoveGameTrackingCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid GameId { get; set; }
    
    public string Platform { get; set; }
}

public class RemoveGameTrackingValidator : AbstractValidator<RemoveGameTrackingCommand>
{
    public RemoveGameTrackingValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Platform).NotEmpty();
    }
}

public class RemoveGameTrackingHandler : IRequestHandler<RemoveGameTrackingCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;

    public RemoveGameTrackingHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<Unit> Handle(RemoveGameTrackingCommand command, CancellationToken cancellationToken)
    {
        var validator = new RemoveGameTrackingValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        GameTracking? gameTracking = await _databaseContext.GameTrackings
            .Where(gt => gt.GameId.Equals(command.GameId) 
                         && gt.UserId.Equals(command.UserId)
                         && gt.Platform.Equals(command.Platform))
            .FirstOrDefaultAsync(cancellationToken);

        if (gameTracking == null)
        {
            throw new NotFoundException("Game tracking not found!");
        }

        _databaseContext.GameTrackings.Remove(gameTracking);

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
            Action = ActivityAction.RemoveTracking
        };
        _databaseContext.Activities.Add(userActivity);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}