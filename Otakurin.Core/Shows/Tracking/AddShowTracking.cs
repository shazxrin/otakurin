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

public class AddShowTrackingCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid ShowId { get; set; }
    
    public int EpisodesWatched { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}

public class AddShowTrackingValidator : AbstractValidator<AddShowTrackingCommand>
{
    public AddShowTrackingValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ShowId).NotEmpty();
    }
}

public class AddShowTrackingMappings : Profile
{
    public AddShowTrackingMappings()
    {
        CreateMap<AddShowTrackingCommand, ShowTracking>();
    }
}

public class AddShowTrackingHandler : IRequestHandler<AddShowTrackingCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;
    
    public AddShowTrackingHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<Unit> Handle(AddShowTrackingCommand command, CancellationToken cancellationToken)
    {
        var validator = new AddShowTrackingValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        // Verify user.
        bool isUserExists = await _databaseContext.Users
            .AsNoTracking()
            .Where(user => user.Id.Equals(command.UserId))
            .AnyAsync(cancellationToken);

        if (!isUserExists)
        {
            throw new NotFoundException("User not found!");
        }

        // Verify if tracked show already exist.
        bool isShowTrackingExists = await _databaseContext.ShowTrackings
            .AsNoTracking()
            .Where(st => st.ShowId.Equals(command.ShowId) 
                         && st.UserId.Equals(command.UserId))
            .AnyAsync(cancellationToken);

        if (isShowTrackingExists)
        {
            throw new ExistsException("Show tracking already exists!");
        }
        
        var show = await _databaseContext.Shows
            .AsNoTracking()
            .Where(show => show.Id.Equals(command.ShowId))
            .FirstOrDefaultAsync(cancellationToken);
        if (show == null)
        {
            throw new NotFoundException("Show not found!");
        }
        
        var showTracking = _mapper.Map<AddShowTrackingCommand, ShowTracking>(command);
        _databaseContext.ShowTrackings.Add(showTracking);
        
        var userActivity = new UserActivity
        {
            UserId = showTracking.UserId,
            Status = showTracking.Status,
            NoOf = showTracking.EpisodesWatched,
            MediaId = show.Id,
            MediaTitle = show.Title,
            MediaCoverImageURL = show.CoverImageURL,
            MediaType = ActivityMediaType.Show,
            Action = ActivityAction.AddTracking
        };
        _databaseContext.Activities.Add(userActivity);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}