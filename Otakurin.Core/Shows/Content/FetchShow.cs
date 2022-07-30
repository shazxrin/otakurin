using AutoMapper;
using Otakurin.Domain.Media;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using Otakurin.Service.Show;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Shows.Content;

public class FetchShowCommand : IRequest<FetchShowResult>
{
    public string ShowRemoteId { get; set; }
}

public class FetchShowValidator : AbstractValidator<FetchShowCommand>
{
    public FetchShowValidator()
    {
        RuleFor(c => c.ShowRemoteId).NotEmpty();
    }
}

public class FetchShowResult
{
    public Guid ShowId { get; set; }
}

public class FetchShowMappings : Profile
{
    public FetchShowMappings()
    {
        CreateMap<APIShow, Show>()
            .ForMember(show => show.Id,
                options => options.Ignore())
            .ForMember(
                show => show.RemoteId,
                options => options.MapFrom(apiShow => apiShow.Id));
    }
}

public class FetchShowHandler : IRequestHandler<FetchShowCommand, FetchShowResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IShowService _showService;
    private readonly IMapper _mapper;

    public FetchShowHandler(DatabaseContext databaseContext, IShowService showService, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _showService = showService;
        _mapper = mapper;
    }
    
    public async Task<FetchShowResult> Handle(FetchShowCommand command, CancellationToken cancellationToken)
    {
        var validator = new FetchShowValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        var dbShow = await _databaseContext.Shows.AsNoTracking()
            .FirstOrDefaultAsync(s => s.RemoteId.Equals(command.ShowRemoteId), cancellationToken);

        if (dbShow != null)
        {
            return new FetchShowResult { ShowId = dbShow.Id };
        }
        
        var apiShow = await _showService.GetShowById(command.ShowRemoteId);

        if (apiShow == null)
        {
            throw new NotFoundException("API show not found");
        }

        var newDBShow = _mapper.Map<APIShow, Show>(apiShow);
        _databaseContext.Shows.Add(newDBShow);
        await _databaseContext.SaveChangesAsync(cancellationToken);

        return new FetchShowResult { ShowId = newDBShow.Id };
    }
}