using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Books.Tracking;

public class RemoveBookTrackingCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid BookId { get; set; }
}

public class RemoveBookTrackingValidator : AbstractValidator<RemoveBookTrackingCommand>
{
    public RemoveBookTrackingValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.BookId).NotEmpty();
    }
}

public class RemoveBookTrackingHandler : IRequestHandler<RemoveBookTrackingCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;

    public RemoveBookTrackingHandler(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async Task<Unit> Handle(RemoveBookTrackingCommand command, CancellationToken cancellationToken)
    {
        var validator = new RemoveBookTrackingValidator();
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
            throw new NotFoundException("Book tracking not found!");
        }
        
        var book = await _databaseContext.Books
            .AsNoTracking()
            .Where(b => b.Id.Equals(command.BookId))
            .FirstOrDefaultAsync(cancellationToken);
        if (book == null)
        {
            throw new NotFoundException("Book not found!");
        }
        
        _databaseContext.BookTrackings.Remove(bookTracking);

        var userActivity = new UserActivity
        {
            UserId = bookTracking.UserId,
            Status = bookTracking.Status,
            NoOf = bookTracking.ChaptersRead,
            MediaId = book.Id,
            MediaTitle = book.Title,
            MediaCoverImageURL = book.CoverImageURL,
            MediaType = ActivityMediaType.Book,
            Action = ActivityAction.RemoveTracking
        };
        _databaseContext.Activities.Add(userActivity);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}