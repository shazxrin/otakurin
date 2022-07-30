using AutoMapper;
using AutoMapper.QueryableExtensions;
using Otakurin.Domain.Tracking;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Shows.Tracking;

public class GetShowTrackingQuery : IRequest<GetShowTrackingResult?>
{
    public Guid UserId { get; set; }
    
    public Guid ShowId { get; set; }
}

public class GetShowTrackingValidator : AbstractValidator<GetShowTrackingQuery>
{
    public GetShowTrackingValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.ShowId).NotEmpty();
    }
}

public class GetShowTrackingResult
{
    public int EpisodesWatched { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}

public class GetShowTrackingMappings : Profile
{
    public GetShowTrackingMappings()
    {
        CreateMap<ShowTracking, GetShowTrackingResult>();
    }
}

public class GetShowTrackingHandler : IRequestHandler<GetShowTrackingQuery, GetShowTrackingResult?>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public GetShowTrackingHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<GetShowTrackingResult?> Handle(GetShowTrackingQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetShowTrackingValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var showTracking = await _databaseContext.ShowTrackings
            .AsNoTracking()
            .Where(st => st.UserId.Equals(query.UserId) 
                                   && st.ShowId.Equals(query.ShowId))
            .ProjectTo<GetShowTrackingResult>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return showTracking;
    }
}