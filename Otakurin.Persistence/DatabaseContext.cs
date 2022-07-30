using Microsoft.EntityFrameworkCore;
using Otakurin.Domain;
using Otakurin.Domain.Media;
using Otakurin.Domain.Pricing;
using Otakurin.Domain.Tracking;
using Otakurin.Domain.User;
using Otakurin.Domain.Wishlist;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Otakurin.Persistence;

#nullable disable

public class DatabaseContext : IdentityDbContext<UserAccount, UserRole, Guid>
{
    public virtual DbSet<UserProfile> UserProfiles { get; set; }
    
    public virtual DbSet<UserActivity> Activities { get; set; }
    
    public virtual DbSet<Game> Games { get; set; }
    
    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<GameTracking> GameTrackings { get; set; }
    
    public virtual DbSet<GameWishlist> GameWishlists { get; set; }
    
    public virtual DbSet<BookTracking> BookTrackings { get; set; }
    
    public virtual DbSet<BookWishlist> BookWishlists { get; set; }

    public virtual DbSet<Show> Shows { get; set; }
    
    public virtual DbSet<ShowTracking> ShowTrackings { get; set; }

    public DatabaseContext() { }
    
    public DatabaseContext(DbContextOptions<DatabaseContext> options): base(options) { }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }
        
    private void UpdateAuditFields()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = now;
                    entry.Entity.LastModifiedOn = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedOn = now;
                    break;
            }
        }
    }
}