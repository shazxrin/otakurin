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

public class AddBookTrackingCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    
    public Guid BookId { get; set; }
    
    public int ChaptersRead { get; set; }
    
    public MediaTrackingFormat Format { get; set; }
    
    public MediaTrackingStatus Status { get; set; }
    
    public MediaTrackingOwnership Ownership { get; set; }
}

public class AddBookTrackingValidator : AbstractValidator<AddBookTrackingCommand>
{
    public AddBookTrackingValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.BookId).NotEmpty();
    }
}

public class AddBookTrackingMappings : Profile
{
    public AddBookTrackingMappings()
    {
        CreateMap<AddBookTrackingCommand, BookTracking>();
    }
}

public class AddBookTrackingHandler : IRequestHandler<AddBookTrackingCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    private readonly IMapper _mapper;

    public AddBookTrackingHandler(DatabaseContext databaseContext, IMapper mapper)
    {
        _databaseContext = databaseContext;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(AddBookTrackingCommand command, CancellationToken cancellationToken)
    {
        var validator = new AddBookTrackingValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(validationResult.Errors);
        }
        
        // Verify user.
        bool isUserExists = await _databaseContext.Users
            .AsNoTracking()
            .Where(u => u.Id.Equals(command.UserId))
            .AnyAsync(cancellationToken);

        if (!isUserExists)
        {
            throw new NotFoundException("User not found!");
        }
        
        // Verify if tracked book already exist.
        bool isBookTrackingExists = await _databaseContext.BookTrackings
            .AsNoTracking()
            .Where(bt => bt.BookId.Equals(command.BookId) 
                         && bt.UserId.Equals(command.UserId))
            .AnyAsync(cancellationToken);

        if (isBookTrackingExists)
        {
            throw new ExistsException("Book tracking already exists!");
        }
        
        // Verify book id.
        var book = await _databaseContext.Books
            .AsNoTracking()
            .Where(b => b.Id.Equals(command.BookId))
            .FirstOrDefaultAsync(cancellationToken);
        if (book == null)
        {
            throw new NotFoundException("Book not found!");
        }
        
        var bookTracking = _mapper.Map<AddBookTrackingCommand, BookTracking>(command);
        _databaseContext.BookTrackings.Add(bookTracking);

        var userActivity = new UserActivity
        {
            UserId = bookTracking.UserId,
            Status = bookTracking.Status,
            NoOf = bookTracking.ChaptersRead,
            MediaId = book.Id,
            MediaTitle = book.Title,
            MediaCoverImageURL = book.CoverImageURL,
            MediaType = ActivityMediaType.Book,
            Action = ActivityAction.AddTracking
        };
        _databaseContext.Activities.Add(userActivity);
        
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}
