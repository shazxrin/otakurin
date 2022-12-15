using AutoMapper;
using AutoMapper.QueryableExtensions;
using Otakurin.Domain.Wishlist;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Wishlist;

public class GetGameWishlistsQuery : IRequest<GetGameWishlistsResult>
{
    public Guid UserId { get; set; } = Guid.Empty;

    public Guid GameId { get; set; } = Guid.Empty;
}

public class GetGameWishlistsValidator : AbstractValidator<GetGameWishlistsQuery>
{
    public GetGameWishlistsValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }
}

public class GetGameWishlistsResult
{
    public class GetGameWishlistItemResult
    {
        public string Platform { get; set; } = string.Empty;
    }

    public List<GetGameWishlistItemResult> Items { get; set; } = new();
};

public class GetGameWishlistsMappings : Profile
{
    public GetGameWishlistsMappings()
    {
        CreateMap<GameWishlist, GetGameWishlistsResult.GetGameWishlistItemResult>();
    }
}

public class GetGameWishlistsHandler : IRequestHandler<GetGameWishlistsQuery, GetGameWishlistsResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public GetGameWishlistsHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<GetGameWishlistsResult> Handle(GetGameWishlistsQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetGameWishlistsValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
        var gameWishlists = await _databaseContext.GameWishlists
            .AsNoTracking()
            .Where(gw => gw.UserId.Equals(query.UserId) && gw.GameId.Equals(query.GameId))
            .ProjectTo<GetGameWishlistsResult.GetGameWishlistItemResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        
        return new GetGameWishlistsResult
        {
            Items = gameWishlists
        };
    }
}