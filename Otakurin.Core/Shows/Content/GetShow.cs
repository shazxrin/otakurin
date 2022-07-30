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

public class GetShowQuery : IRequest<GetShowResult>
{
    public Guid ShowId { get; init; }
}

public class GetShowValidator : AbstractValidator<GetShowQuery>
{
    public GetShowValidator()
    {
        RuleFor(q => q.ShowId).NotEmpty();
    }
}

public class GetShowResult
{
    public Guid Id { get; init; }
    
    public string CoverImageURL { get; init; }
    
    public string Title { get; init; }
    
    public string Summary { get; init; }
    
    public ShowType ShowType { get; init; }
}

public class GetShowMappings : Profile
{
    public GetShowMappings()
    {
        CreateMap<Show, GetShowResult>();
    }
}

public class GetShowHandler : IRequestHandler<GetShowQuery, GetShowResult>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IShowService _showService;
    private readonly IMapper _mapper;
    
    public GetShowHandler(DatabaseContext databaseContext, IShowService showService, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _showService = showService;
        _mapper = mapper;
    }
    
    public async Task<GetShowResult> Handle(GetShowQuery query, CancellationToken cancellationToken)
    {
        var validator = new GetShowValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        // Find show from local database
        var dbShow = await _databaseContext.Shows
            .Where(show => show.Id.Equals(query.ShowId))
            .FirstOrDefaultAsync(cancellationToken);

        if (dbShow == null)
        {
            throw new NotFoundException("Show not found!");
        }
        
        // Return cached show if its fresh.
        var timeSpan = DateTime.Now - dbShow.LastModifiedOn;
        if (timeSpan?.TotalHours > 12)
        {
            var remoteShow = await _showService.GetShowById(dbShow.RemoteId);
            if (remoteShow != null)
            {
                _mapper.Map<APIShow, Show>(remoteShow, dbShow);
                _databaseContext.Shows.Update(dbShow);
                await _databaseContext.SaveChangesAsync(cancellationToken);
            }

        }
        
        return _mapper.Map<Show, GetShowResult>(dbShow);
    }
}
