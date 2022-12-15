using AutoMapper;
using Otakurin.Domain.Media;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using Otakurin.Service.Game;

namespace Otakurin.Core.Games.Content;

public class FetchGameCommand : IRequest<FetchGameResult>
{
    public long GameRemoteId { get; set; } = 0L;
}

public class FetchGameResult
{
    public Guid GameId { get; set; }
}

public class FetchGameMappings : Profile
{
    public FetchGameMappings()
    {
        CreateMap<APIGame, Game>()
            .ForMember(game => game.Id,
                options => options.Ignore())
            .ForMember(
                game => game.RemoteId,
                options => options.MapFrom(apiGame => apiGame.Id))
            .ForMember(
                game => game.ScreenshotsUrlsString,
                options => options.MapFrom(apiGame => string.Join(";", apiGame.ScreenshotsUrls)))
            .ForMember(
                game => game.PlatformsString,
                options => options.MapFrom(apiGame => string.Join(";", apiGame.Platforms)))
            .ForMember(
                game => game.CompaniesString,
                options => options.MapFrom(apiGame => string.Join(";", apiGame.Companies)));
    }
}

public class FetchGameHandler : IRequestHandler<FetchGameCommand, FetchGameResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IGameService _gameService;
    private readonly IMapper _mapper;

    public FetchGameHandler(DatabaseContext databaseContext, IGameService gameService, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _gameService = gameService;
        _mapper = mapper;
    }
    
    public async Task<FetchGameResult> Handle(FetchGameCommand command, CancellationToken cancellationToken)
    {
        var dbGame = await _databaseContext.Games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.RemoteId.Equals(command.GameRemoteId), cancellationToken);

        if (dbGame != null)
        {
            return new FetchGameResult { GameId = dbGame.Id };
        }
        
        var apiGame = await _gameService.GetGameById(command.GameRemoteId);

        if (apiGame == null)
        {
            throw new NotFoundException("API game not found");
        }

        var newDBGame = _mapper.Map<APIGame, Game>(apiGame);
        _databaseContext.Games.Add(newDBGame);
        await _databaseContext.SaveChangesAsync(cancellationToken);

        return new FetchGameResult { GameId = newDBGame.Id };
    }
}