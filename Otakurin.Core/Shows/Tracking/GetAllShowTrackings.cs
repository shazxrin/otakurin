using AutoMapper;
using Otakurin.Domain.Media;
using Otakurin.Domain.Tracking;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Common;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Shows.Tracking;

public class GetAllShowTrackingsQuery : PagedListRequest, IRequest<PagedListResult<GetAllShowTrackingsItemResult>>
{
    public Guid UserId { get; set; }
    
    public MediaTrackingStatus? Status { get; set; } = null;

    public bool SortByRecentlyModified { get; set; } = false;
    
    public bool SortByEpisodesWatched { get; set; } = false;
    
    public bool SortByFormat { get; set; } = false;
    
    public bool SortByOwnership { get; set; } = false;
}

public class GetAllShowTrackingsValidator : AbstractValidator<GetAllShowTrackingsQuery>
{
    public GetAllShowTrackingsValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }
}

public class GetAllShowTrackingsItemResult
{
    public Guid ShowId { get; set; }
    
    public string Title { get; set; }
    
    public string CoverImageURL { get; set; }
    
    public int EpisodesWatched { get; set; }
    
    public ShowType ShowType { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}

public class GetAllShowTrackingsHandler : IRequestHandler<GetAllShowTrackingsQuery, PagedListResult<GetAllShowTrackingsItemResult>>
{
    private readonly DatabaseContext _databaseContext;

    public GetAllShowTrackingsHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<PagedListResult<GetAllShowTrackingsItemResult>> Handle(GetAllShowTrackingsQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetAllShowTrackingsValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var queryable = _databaseContext.ShowTrackings
            .AsNoTracking()
            .Where(st => st.UserId.Equals(query.UserId));
        
        if (query.Status != null) queryable = queryable.Where(st => st.Status == query.Status);
        if (query.SortByRecentlyModified) queryable = queryable.OrderByDescending(st => st.LastModifiedOn);
        if (query.SortByEpisodesWatched) queryable = queryable.OrderBy(st => st.EpisodesWatched);
        if (query.SortByFormat) queryable = queryable.OrderBy(st => st.Format);
        if (query.SortByOwnership) queryable = queryable.OrderBy(st => st.Ownership);

        var joinQueryable = queryable.Join(
            _databaseContext.Shows,
            st => st.ShowId,
            s => s.Id,
            (st, s) => new GetAllShowTrackingsItemResult
            {
                ShowId = s.Id,
                Title = s.Title,
                CoverImageURL = s.CoverImageURL,
                EpisodesWatched = st.EpisodesWatched,
                ShowType = s.ShowType,
                Format = st.Format,
                Status = st.Status,
                Ownership = st.Ownership
            }
        );
        
        var pagedList = await PagedListResult<GetAllShowTrackingsItemResult>.CreateAsync(
            joinQueryable,
            query.Page,
            query.PageSize,
            cancellationToken
        );

        return pagedList;
    }
}