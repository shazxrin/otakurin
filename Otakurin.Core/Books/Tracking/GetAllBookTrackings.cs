using Otakurin.Domain.Tracking;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Common;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Tracking;

public class GetAllBookTrackingsQuery : PagedListRequest, IRequest<PagedListResult<GetAllBookTrackingsItemResult>>
{
    public Guid UserId { get; set; }
    
    public MediaTrackingStatus? Status { get; set; } = null;

    public bool SortByRecentlyModified { get; set; } = false;
    
    public bool SortByChaptersRead { get; set; } = false;

    public bool SortByFormat { get; set; } = false;
    
    public bool SortByOwnership { get; set; } = false;
}

public class GetAllBookTrackingsValidator : AbstractValidator<GetAllBookTrackingsQuery>
{
    public GetAllBookTrackingsValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }    
}

public class GetAllBookTrackingsItemResult
{
    public Guid BookId { get; set; }
    
    public string Title { get; set; }
    
    public string CoverImageURL { get; set; }
    
    public int ChaptersRead { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}

public class GetAllBookTrackingsHandler : IRequestHandler<GetAllBookTrackingsQuery, PagedListResult<GetAllBookTrackingsItemResult>>
{
    private readonly DatabaseContext _databaseContext;

    public GetAllBookTrackingsHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<PagedListResult<GetAllBookTrackingsItemResult>> Handle(GetAllBookTrackingsQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetAllBookTrackingsValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var queryable = _databaseContext.BookTrackings
            .AsNoTracking()
            .Where(bt => bt.UserId.Equals(query.UserId));

        if (query.Status != null) queryable = queryable.Where(bt => bt.Status == query.Status);
        if (query.SortByRecentlyModified) queryable = queryable.OrderByDescending(bt => bt.LastModifiedOn);
        if (query.SortByChaptersRead) queryable = queryable.OrderBy(bt => bt.ChaptersRead);
        if (query.SortByFormat) queryable = queryable.OrderBy(bt => bt.Format);
        if (query.SortByOwnership) queryable = queryable.OrderBy(bt => bt.Ownership);

        var joinQueryable = queryable.Join(
            _databaseContext.Books,
            bt => bt.BookId,
            b => b.Id,
            (bt, b) => new GetAllBookTrackingsItemResult 
            {
                BookId = b.Id,
                Title = b.Title,
                CoverImageURL = b.CoverImageURL,
                ChaptersRead = bt.ChaptersRead,
                Format = bt.Format,
                Status = bt.Status,
                Ownership = bt.Ownership
            }
        );
        
        var pagedList = await PagedListResult<GetAllBookTrackingsItemResult>.CreateAsync(
            joinQueryable,
            query.Page,
            query.PageSize,
            cancellationToken
        );

        return pagedList;
    }
}