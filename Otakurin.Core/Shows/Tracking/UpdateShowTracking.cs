using AutoMapper;
using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Shows.Tracking;

public class UpdateShowTrackingCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid ShowId { get; set; }
    
    public int EpisodesWatched { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}

public class UpdateShowTrackingValidator : AbstractValidator<UpdateShowTrackingCommand>
{
    public UpdateShowTrackingValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ShowId).NotEmpty();
    }
}

public class UpdateShowTrackingMappings : Profile
{
    public UpdateShowTrackingMappings()
    {
        CreateMap<UpdateShowTrackingCommand, ShowTracking>();
    }
}

public class UpdateShowTrackingHandler : IRequestHandler<UpdateShowTrackingCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public UpdateShowTrackingHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<Unit> Handle(UpdateShowTrackingCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateShowTrackingValidator();
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
            throw new NotFoundException();
        }
        
        var show = await _databaseContext.Shows
            .AsNoTracking()
            .Where(s => s.Id.Equals(command.ShowId))
            .FirstOrDefaultAsync(cancellationToken);
        if (show == null)
        {
            throw new NotFoundException("Show not found!");
        }

        _mapper.Map<UpdateShowTrackingCommand, ShowTracking>(command, showTracking);
        _databaseContext.ShowTrackings.Update(showTracking);
        
        var userActivity = new UserActivity
        {
            UserId = showTracking.UserId,
            Status = showTracking.Status,
            NoOf = showTracking.EpisodesWatched,
            MediaId = show.Id,
            MediaTitle = show.Title,
            MediaCoverImageURL = show.CoverImageURL,
            MediaType = ActivityMediaType.Show,
            Action = ActivityAction.UpdateTracking
        };
        _databaseContext.Activities.Add(userActivity);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}