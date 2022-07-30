using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;

namespace Otakurin.Core.Users.Activity;

public class GetUserActivitiesQuery : IRequest<GetUserActivitiesResult>
{
    public Guid UserId { get; set; }
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
        public Guid ActivityId { get; set; }
        
        public string UserName { get; set; }
        
        public string ProfilePictureURL { get; set; }
        
        public Guid MediaId { get; set; }
        
        public string MediaTitle { get; set; }
        
        public string MediaCoverImageURL { get; set; }
        
        public ActivityMediaType MediaType { get; set; }
        
        public MediaTrackingStatus? Status { get; set; }
        
        public int NoOf { get; set; }
        
        public ActivityAction Action { get; set; }
        
        public DateTime DateTime { get; set; }
    }

    public List<GetUserActivitiesItemResult> Items { get; set; }
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
                UserName = user.UserName,
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