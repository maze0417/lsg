using LSG.Core.Entities;
using LSG.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace LSG.Infrastructure.DataServices;

public interface ILsgReadOnlyRepository : IBaseRepository
{
    DbSet<Schema> Schemas { get; set; }
}

public sealed class LsgReadOnlyRepository : BaseRepository, ILsgReadOnlyRepository
{
    public LsgReadOnlyRepository(DbContextOptions<LsgReadOnlyRepository> options) : base(options)
    {
    }

    public DbSet<Schema> Schemas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyAllConfigurations();
        modelBuilder.CascadeAllRelationsOnDelete();
    }
}