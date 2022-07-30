using AutoMapper;
using AutoMapper.QueryableExtensions;
using Otakurin.Domain.Tracking;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Tracking;

public class GetBookTrackingQuery : IRequest<GetBookTrackingResult?>
{
    public Guid UserId { get; set; }
    
    public Guid BookId { get; set; }
}

public class GetBookTrackingValidator : AbstractValidator<GetBookTrackingQuery>
{
    public GetBookTrackingValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.BookId).NotEmpty();
    }    
}

public class GetBookTrackingResult
{
    public int ChaptersRead { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}

public class GetBookTrackingMappings : Profile
{
    public GetBookTrackingMappings()
    {
        CreateMap<BookTracking, GetBookTrackingResult>();
    }
}

public class GetBookTrackingHandler : IRequestHandler<GetBookTrackingQuery, GetBookTrackingResult?>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public GetBookTrackingHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<GetBookTrackingResult?> Handle(GetBookTrackingQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetBookTrackingValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var bookTracking = await _databaseContext.BookTrackings
            .AsNoTracking()
            .Where(bt => bt.UserId.Equals(query.UserId) && bt.BookId.Equals(query.BookId))
            .ProjectTo<GetBookTrackingResult>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return bookTracking;
    }
}
