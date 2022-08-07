using AutoMapper;
using Otakurin.Domain.Media;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using Otakurin.Service.Game;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Games.Content;

public class GetGameQuery : IRequest<GetGameResult>
{
    public Guid GameId { get; set; }
}

public class GetGameValidator : AbstractValidator<GetGameQuery>
{
    public GetGameValidator()
    {
        RuleFor(q => q.GameId).NotEmpty();
    }
}

public class GetGameResult
{
    public Guid Id { get; set; }
    
    public string CoverImageURL { get; set; }
    
    public string Title { get; set; }
    
    public string Summary { get; set; }
    
    public List<string> ScreenshotsUrls { get; set; }
    
    public List<string> Platforms { get; set; }
    
    public List<string> Companies { get; set; }
}

public class GetGameMappings : Profile
{
    public GetGameMappings()
    {
        CreateMap<Game, GetGameResult>()
            .ForMember(
                ggr => ggr.ScreenshotsUrls,
                options => options.MapFrom(game => game.ScreenshotsUrlsString.Split(';', StringSplitOptions.None)))
            .ForMember(
                ggr => ggr.Platforms,
                options => options.MapFrom(game => game.PlatformsString.Split(';', StringSplitOptions.None)))
            .ForMember(
                ggr => ggr.Companies,
                options => options.MapFrom(game => game.CompaniesString.Split(';', StringSplitOptions.None)));
    }
}

public class GetGameHandler : IRequestHandler<GetGameQuery, GetGameResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IGameService _gameService;
    private readonly IMapper _mapper;

    public GetGameHandler(DatabaseContext databaseContext, IGameService gameService, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _gameService = gameService;
        _mapper = mapper;
    }

    public async Task<GetGameResult> Handle(GetGameQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetGameValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        // Find game from database.
        var dbGame = await _databaseContext.Games
            .Where(game => game.Id.Equals(query.GameId))
            .FirstOrDefaultAsync(cancellationToken);

        if (dbGame == null)
        {
            throw new NotFoundException("Game not found");
        }
        
        // Fetch game from remote if cache is stale.
        var timeSpan = DateTime.Now - dbGame.LastModifiedOn;
        if (timeSpan?.TotalHours > 12)
        {
            var remoteGame = await _gameService.GetGameById(dbGame.RemoteId);
            if (remoteGame != null)
            {
                _mapper.Map<APIGame, Game>(remoteGame, dbGame);
                _databaseContext.Games.Update(dbGame);
                await _databaseContext.SaveChangesAsync(cancellationToken);

                return _mapper.Map<Game, GetGameResult>(dbGame);
            }
        }
        
        return _mapper.Map<Game, GetGameResult>(dbGame);
    }
}