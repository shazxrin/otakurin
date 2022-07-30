using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Shows.Tracking;

public class RemoveShowTrackingCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid ShowId { get; set; }
}

public class RemoveShowTrackingValidator : AbstractValidator<RemoveShowTrackingCommand>
{
    public RemoveShowTrackingValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}

public class RemoveShowTrackingHandler : IRequestHandler<RemoveShowTrackingCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    
    public RemoveShowTrackingHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<Unit> Handle(RemoveShowTrackingCommand command, CancellationToken cancellationToken)
    {
        var validator = new RemoveShowTrackingValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        ShowTracking? showTracking = await _databaseContext.ShowTrackings
            .Where(st => st.ShowId.Equals(command.ShowId) 
                                   && st.UserId.Equals(command.UserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (showTracking == null)
        {
            throw new NotFoundException("Show tracking not found!");
        }

        _databaseContext.ShowTrackings.Remove(showTracking);
        
        var show = await _databaseContext.Shows
            .AsNoTracking()
            .Where(s => s.Id.Equals(command.ShowId))
            .FirstOrDefaultAsync(cancellationToken);
        if (show == null)
        {
            throw new NotFoundException("Show not found!");
        }
        
        var userActivity = new UserActivity
        {
            UserId = showTracking.UserId,
            Status = showTracking.Status,
            NoOf = showTracking.EpisodesWatched,
            MediaId = show.Id,
            MediaTitle = show.Title,
            MediaCoverImageURL = show.CoverImageURL,
            MediaType = ActivityMediaType.Show,
            Action = ActivityAction.RemoveTracking
        };
        _databaseContext.Activities.Add(userActivity);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}