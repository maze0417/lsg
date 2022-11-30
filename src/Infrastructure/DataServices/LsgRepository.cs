using LSG.Core.Entities;
using LSG.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace LSG.Infrastructure.DataServices;

public class LsgRepository : BaseRepository, ILsgRepository
{
    public LsgRepository(DbContextOptions<LsgRepository> options) : base(options)
    {
    }


    public DbSet<Schema> Schemas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyAllConfigurations();
        modelBuilder.CascadeAllRelationsOnDelete();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
     
    }
}