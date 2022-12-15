using Otakurin.Domain.User;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Otakurin.Core.Exceptions;
using ValidationException = Otakurin.Core.Exceptions.ValidationException;

namespace Otakurin.Core.Users.Account;

public class SignInUserCommand : IRequest<SignInUserResult>
{
    public string UserName { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
}

public class SignInUserValidator : AbstractValidator<SignInUserCommand>
{
    public SignInUserValidator()
    {
        RuleFor(c => c.UserName).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
    }
}

public class SignInUserResult
{
    public UserAccount User { get; set; } = new();
}

public class SignInUserHandler : IRequestHandler<SignInUserCommand, SignInUserResult>
{
    private readonly UserManager<UserAccount> _userManager;

    public SignInUserHandler(UserManager<UserAccount> userManager)
    {
        _userManager = userManager;
    }
    
    public async Task<SignInUserResult> Handle(SignInUserCommand command, CancellationToken cancellationToken)
    {
        var validator = new SignInUserValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userAccount = await _userManager.FindByNameAsync(command.UserName);
        if (userAccount == null)
        {
            throw new ForbiddenException();
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(userAccount, command.Password);
        if (!isPasswordValid)
        {
            throw new ForbiddenException();
        }
        
        return new SignInUserResult 
        {
            User = userAccount
        };
    }
}
