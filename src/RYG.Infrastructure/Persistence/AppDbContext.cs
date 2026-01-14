namespace RYG.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Equipment> Equipment => Set<Equipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.State).IsRequired();
            entity.Property(e => e.StateChangedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}