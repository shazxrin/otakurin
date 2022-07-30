using AutoMapper;
using AutoMapper.QueryableExtensions;
using Otakurin.Domain.Tracking;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Tracking;

public class GetGameTrackingsQuery : IRequest<GetGameTrackingsResult>
{
    public Guid UserId { get; set; }
    
    public Guid GameId { get; set; }
}

public class GetGameTrackingsValidator : AbstractValidator<GetGameTrackingsQuery>
{
    public GetGameTrackingsValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }    
}

public class GetGameTrackingsResult
{
    public class GetGameTrackingsItemResult
    {
        public int HoursPlayed { get; set; }
        
        public string Platform { get; set; }
        
        public MediaTrackingFormat Format { get; set; }
        
        public MediaTrackingStatus Status { get; set; }
        
        public MediaTrackingOwnership Ownership { get; set; }
    }

    public List<GetGameTrackingsResult.GetGameTrackingsItemResult> Items { get; set; }
}

public class GetGameTrackingsMappings : Profile
{
    public GetGameTrackingsMappings()
    {
        CreateMap<GameTracking, GetGameTrackingsResult.GetGameTrackingsItemResult>();
    }
}

public class GetGameTrackingsHandler : IRequestHandler<GetGameTrackingsQuery, GetGameTrackingsResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public GetGameTrackingsHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<GetGameTrackingsResult> Handle(GetGameTrackingsQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetGameTrackingsValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }

        var gameTrackings = await _databaseContext.GameTrackings
            .AsNoTracking()
            .Where(gt => gt.UserId.Equals(query.UserId) && gt.GameId.Equals(query.GameId))
            .ProjectTo<GetGameTrackingsResult.GetGameTrackingsItemResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        
        return new GetGameTrackingsResult
        {
            Items = gameTrackings
        };
    }
}
