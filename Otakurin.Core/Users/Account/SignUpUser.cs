using System.Text.RegularExpressions;
using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Otakurin.Core.Exceptions;
using Otakurin.Persistence;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Users.Account;

public class SignUpUserCommand : IRequest<Unit>
{
    public string Email { get; set; } = string.Empty;
    
    public string UserName { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
}

public class SignUpUserValidator : AbstractValidator<SignUpUserCommand>
{
    public SignUpUserValidator()
    {
        RuleFor(c => c.Email).EmailAddress();
        RuleFor(c => c.UserName).MinimumLength(6).MaximumLength(20).NotEmpty().Matches(
            new Regex(@"^[a-zA-Z0-9]+$")    
        );
        RuleFor(c => c.Password).Matches(
            new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$")
        );
    }
}

public class SignUpUserHandler : IRequestHandler<SignUpUserCommand, Unit>
{
    private readonly DatabaseContext _databaseContext;
    private readonly UserManager<UserAccount> _userManager;

    public SignUpUserHandler(DatabaseContext databaseContext, UserManager<UserAccount> userManager)
    {
        _databaseContext = databaseContext;
        _userManager = userManager;
    }
    
    public async Task<Unit> Handle(SignUpUserCommand command, CancellationToken cancellationToken)
    {
        var validator = new SignUpUserValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
        var isUserNameTaken = await _databaseContext.Users
            .AnyAsync(u => u.UserName != null && u.UserName.Equals(command.UserName), cancellationToken);
        if (isUserNameTaken)
        {
            throw new ExistsException("User name already exists!");
        }
        
        var isEmailTaken = await _databaseContext.Users
            .AnyAsync(u => u.Email != null && u.Email.Equals(command.Email), cancellationToken);
        if (isEmailTaken)
        {
            throw new ExistsException("Email already exists!");
        }

        var newUser = new UserAccount
        {
            Email = command.Email,
            UserName = command.UserName
        };
        var result = await _userManager.CreateAsync(newUser, command.Password);

        if (!result.Succeeded)
        {
            throw new Exception("Unable to sign up user!");
        }

        _databaseContext.UserProfiles.Add(new UserProfile
        {
            UserId = newUser.Id,
            ProfilePictureURL = "",
            Bio = "",
        });
        await _databaseContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}
