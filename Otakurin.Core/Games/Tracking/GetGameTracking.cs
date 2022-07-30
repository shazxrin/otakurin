using AutoMapper;
using AutoMapper.QueryableExtensions;
using Otakurin.Domain.Tracking;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Tracking;

public class GetGameTrackingQuery : IRequest<GetGameTrackingResult>
{
    public Guid UserId { get; set; } = Guid.Empty;
    
    public Guid GameId { get; set; } = Guid.Empty;

    public string Platform { get; set; } = string.Empty;
}

public class GetGameTrackingValidator : AbstractValidator<GetGameTrackingQuery>
{
    public GetGameTrackingValidator()
    {
        RuleFor(q => q.UserId).NotEqual(Guid.Empty);
        RuleFor(q => q.GameId).NotEqual(Guid.Empty);
        RuleFor(q => q.Platform).NotEqual(string.Empty);
    }    
}

public class GetGameTrackingResult
{
    public int HoursPlayed { get; set; } = 0;
    
    public string Platform { get; set; } = string.Empty;

    public MediaTrackingFormat Format { get; set; } = MediaTrackingFormat.Digital;

    public MediaTrackingStatus Status { get; set; } = MediaTrackingStatus.InProgress;

    public MediaTrackingOwnership Ownership { get; set; } = MediaTrackingOwnership.Owned;
}

public class GetGameTrackingMappings : Profile
{
    public GetGameTrackingMappings()
    {
        CreateMap<GameTracking, GetGameTrackingResult>();
    }
}

public class GetGameTrackingHandler : IRequestHandler<GetGameTrackingQuery, GetGameTrackingResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public GetGameTrackingHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<GetGameTrackingResult> Handle(GetGameTrackingQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetGameTrackingValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }

        var tracking = await _databaseContext.GameTrackings
            .AsNoTracking()
            .Where(gt => gt.UserId.Equals(query.UserId) && gt.GameId.Equals(query.GameId) && gt.Platform.Equals(query.Platform))
            .ProjectTo<GetGameTrackingResult>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (tracking == null)
        {
            throw new NotFoundException();
        }

        return tracking;
    }
}
