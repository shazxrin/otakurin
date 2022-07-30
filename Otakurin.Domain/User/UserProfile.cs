namespace Otakurin.Domain.User;

#nullable disable

public class UserProfile : Entity
{
    public Guid UserId { get; set; }

    public string ProfilePictureURL { get; set; }
    
    public string Bio { get; set; }
}
