using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Users.Activity;

public class GetUserActivitiesQuery : IRequest<GetUserActivitiesResult>
{
    public Guid UserId { get; set; } = Guid.Empty;
}

public class GetUserActivitiesValidator : AbstractValidator<GetUserActivitiesQuery>
{
    public GetUserActivitiesValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }
}

public class GetUserActivitiesResult
{
    public class GetUserActivitiesItemResult
    {
        public Guid ActivityId { get; set; } = Guid.Empty;
        
        public string UserName { get; set; } = string.Empty;
        
        public string ProfilePictureURL { get; set; } = string.Empty;
        
        public Guid MediaId { get; set; } = Guid.Empty;
        
        public string MediaTitle { get; set; } = string.Empty;
        
        public string MediaCoverImageURL { get; set; } = string.Empty;

        public ActivityMediaType MediaType { get; set; } = ActivityMediaType.Game;

        public MediaTrackingStatus? Status { get; set; } = MediaTrackingStatus.InProgress;

        public int NoOf { get; set; } = 0;

        public ActivityAction Action { get; set; } = ActivityAction.AddTracking;
        
        public DateTime DateTime { get; set; } = DateTime.Now;
    }

    public List<GetUserActivitiesItemResult> Items { get; set; } = new();
}

public class GetUserActivitiesHandler : IRequestHandler<GetUserActivitiesQuery, GetUserActivitiesResult>
{
    private readonly DatabaseContext _databaseContext;

    public GetUserActivitiesHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<GetUserActivitiesResult> Handle(GetUserActivitiesQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetUserActivitiesValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
        var user = await _databaseContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id.Equals(query.UserId), cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User not found!");
        }

        var userProfile = await _databaseContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId.Equals(query.UserId), cancellationToken);
        if (userProfile == null)
        {
            throw new NotFoundException("User profile not found!");
        }
        
        var activities = await _databaseContext.Activities
            .AsNoTracking()
            .Where(a => a.UserId.Equals(query.UserId))
            .OrderByDescending(a => a.CreatedOn)
            .Take(10)
            .Select(activity => new GetUserActivitiesResult.GetUserActivitiesItemResult
            {
                ActivityId = activity.Id,
                UserName = user.UserName ?? string.Empty,
                ProfilePictureURL = userProfile.ProfilePictureURL,
                MediaId = activity.MediaId,
                MediaTitle = activity.MediaTitle,
                MediaCoverImageURL = activity.MediaCoverImageURL,
                MediaType = activity.MediaType,
                Status = activity.Status,
                NoOf = activity.NoOf,
                Action = activity.Action,
                DateTime = activity.CreatedOn
            })
            .ToListAsync(cancellationToken);
        
        return new GetUserActivitiesResult 
        {
            Items = activities
        };
    }
}