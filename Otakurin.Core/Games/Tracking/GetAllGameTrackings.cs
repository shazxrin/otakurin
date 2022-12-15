using Otakurin.Domain.Tracking;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Common;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Tracking;

public class GetAllGameTrackingsQuery : PagedListRequest, IRequest<PagedListResult<GetAllGameTrackingsItemResult>>
{
    public Guid UserId { get; set; } = Guid.Empty;
    
    public MediaTrackingStatus? Status { get; set; } = null;

    public bool SortByRecentlyModified { get; set; } = false;
    
    public bool SortByHoursPlayed { get; set; } = false;
    
    public bool SortByPlatform { get; set; } = false;
    
    public bool SortByFormat { get; set; } = false;
    
    public bool SortByOwnership { get; set; } = false;
}

public class GetAllGameTrackingsValidator : AbstractValidator<GetAllGameTrackingsQuery>
{
    public GetAllGameTrackingsValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }    
}

public class GetAllGameTrackingsItemResult
{
    public Guid GameId { get; set; } = Guid.Empty;

    public string Title { get; set; } = string.Empty;

    public string CoverImageURL { get; set; } = string.Empty;

    public float HoursPlayed { get; set; } = 0f;

    public string Platform { get; set; } = string.Empty;

    public MediaTrackingFormat Format { get; set; } = MediaTrackingFormat.Digital;

    public MediaTrackingStatus Status { get; set; } = MediaTrackingStatus.InProgress;

    public MediaTrackingOwnership Ownership { get; set; } = MediaTrackingOwnership.Owned;
}

public class GetAllGameTrackingsHandler : IRequestHandler<GetAllGameTrackingsQuery, PagedListResult<GetAllGameTrackingsItemResult>>
{
    private readonly DatabaseContext _databaseContext;

    public GetAllGameTrackingsHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<PagedListResult<GetAllGameTrackingsItemResult>> Handle(GetAllGameTrackingsQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetAllGameTrackingsValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
        var queryable = _databaseContext.GameTrackings
            .AsNoTracking()
            .Where(gt => gt.UserId.Equals(query.UserId));

        if (query.Status != null) queryable = queryable.Where(gt => gt.Status == query.Status);
        if (query.SortByRecentlyModified) queryable = queryable.OrderByDescending(gt => gt.LastModifiedOn);
        if (query.SortByHoursPlayed) queryable = queryable.OrderBy(gt => gt.HoursPlayed);
        if (query.SortByPlatform) queryable = queryable.OrderBy(gt => gt.Platform);
        if (query.SortByFormat) queryable = queryable.OrderBy(gt => gt.Format);
        if (query.SortByOwnership) queryable = queryable.OrderBy(gt => gt.Ownership);

        var joinQueryable = queryable.Join(
            _databaseContext.Games,
            gt => gt.GameId,
            g => g.Id,
            (gt, g) => new GetAllGameTrackingsItemResult 
            {
                GameId = g.Id,
                Title = g.Title,
                CoverImageURL = g.CoverImageURL,
                HoursPlayed = gt.HoursPlayed,
                Platform = gt.Platform,
                Format = gt.Format,
                Status = gt.Status,
                Ownership = gt.Ownership
            }
        );
        
        var pagedList = await PagedListResult<GetAllGameTrackingsItemResult>.CreateAsync(
            joinQueryable,
            query.Page,
            query.PageSize,
            cancellationToken
        );

        return pagedList;
    }
}