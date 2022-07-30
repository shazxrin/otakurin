using AutoMapper;
using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Tracking;

public class UpdateBookTrackingCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid BookId { get; set; }
    
    public int ChaptersRead { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}

public class UpdateBookTrackingValidator : AbstractValidator<UpdateBookTrackingCommand>
{
    public UpdateBookTrackingValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.BookId).NotEmpty();
    }
}

public class UpdateBookTrackingMappings : Profile
{
    public UpdateBookTrackingMappings()
    {
        CreateMap<UpdateBookTrackingCommand, BookTracking>();
    }
}

public class UpdateBookTrackingHandler : IRequestHandler<UpdateBookTrackingCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public UpdateBookTrackingHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }
    
    public async Task<Unit> Handle(UpdateBookTrackingCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateBookTrackingValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        BookTracking? bookTracking = await _databaseContext.BookTrackings
            .Where(bt => bt.BookId.Equals(command.BookId) 
                         && bt.UserId.Equals(command.UserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (bookTracking == null)
        {
            throw new NotFoundException();
        }
        
        var book = await _databaseContext.Books
            .AsNoTracking()
            .Where(b => b.Id.Equals(command.BookId))
            .FirstOrDefaultAsync(cancellationToken);
        if (book == null)
        {
            throw new NotFoundException("Book not found!");
        }
        
        _mapper.Map<UpdateBookTrackingCommand, BookTracking>(command, bookTracking);
        _databaseContext.BookTrackings.Update(bookTracking);

        var userActivity = new UserActivity
        {
            UserId = bookTracking.UserId,
            Status = bookTracking.Status,
            NoOf = bookTracking.ChaptersRead,
            MediaId = book.Id,
            MediaTitle = book.Title,
            MediaCoverImageURL = book.CoverImageURL,
            MediaType = ActivityMediaType.Book,
            Action = ActivityAction.UpdateTracking
        };
        _databaseContext.Activities.Add(userActivity);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}