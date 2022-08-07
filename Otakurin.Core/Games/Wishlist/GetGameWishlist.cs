using AutoMapper;
using AutoMapper.QueryableExtensions;
using Otakurin.Domain.Wishlist;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Wishlist;

public class GetGameWishlistQuery : IRequest<GetGameWishlistResult>
{
    public Guid UserId { get; set; } = Guid.Empty;

    public Guid GameId { get; set; } = Guid.Empty;

    public string Platform { get; set; } = string.Empty;
}

public class GetGameWishlistValidator : AbstractValidator<GetGameWishlistQuery>
{
    public GetGameWishlistValidator()
    {
        RuleFor(q => q.UserId).NotEqual(Guid.Empty);
        RuleFor(q => q.GameId).NotEqual(Guid.Empty);
        RuleFor(q => q.Platform).NotEmpty();
    }
}

public class GetGameWishlistResult
{
    public string Platform { get; set; }
};

public class GetGameWishlistHandler : IRequestHandler<GetGameWishlistQuery, GetGameWishlistResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public GetGameWishlistHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task<GetGameWishlistResult> Handle(GetGameWishlistQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetGameWishlistValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var isExist = await _databaseContext.GameWishlists
            .AsNoTracking()
            .AnyAsync(
                gw => gw.UserId.Equals(query.UserId) && gw.GameId.Equals(query.GameId) &&
                      gw.Platform.Equals(query.Platform), cancellationToken);

        if (!isExist)
        {
            throw new NotFoundException();
        }

        return new GetGameWishlistResult()
        {
            Platform = query.Platform
        };
    }
}