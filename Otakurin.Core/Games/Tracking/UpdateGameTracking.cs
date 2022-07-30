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

public class UpdateGameTrackingCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    
    public Guid GameId { get; set; }
    
    public string Platform { get; set; }
    
    public float HoursPlayed { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }

    public MediaTrackingOwnership Ownership { get; set; }
}

public class UpdateGameTrackingValidator : AbstractValidator<UpdateGameTrackingCommand>
{
    public UpdateGameTrackingValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.GameId).NotEmpty();
        RuleFor(c => c.Platform).NotEmpty();
    }
}

public class UpdateGameTrackingMappings : Profile
{
    public UpdateGameTrackingMappings()
    {
        CreateMap<UpdateGameTrackingCommand, GameTracking>();
    }
}

public class UpdateGameTrackingHandler : IRequestHandler<UpdateGameTrackingCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public UpdateGameTrackingHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<Unit> Handle(UpdateGameTrackingCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateGameTrackingValidator();
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
            throw new NotFoundException();
        }

        _mapper.Map<UpdateGameTrackingCommand, GameTracking>(command, gameTracking);
        _databaseContext.GameTrackings.Update(gameTracking);
        
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
            Action = ActivityAction.UpdateTracking
        };
        _databaseContext.Activities.Add(userActivity);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}