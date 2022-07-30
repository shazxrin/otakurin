using System.ComponentModel.DataAnnotations;

namespace Otakurin.Domain;

#nullable disable

public abstract class Entity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedOn { get; set; }

    public DateTime? LastModifiedOn { get; set; }
}