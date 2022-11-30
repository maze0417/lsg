using LSG.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LSG.Infrastructure.DataServices;

public interface ILsgRepository : IBaseRepository
{
    DbSet<Schema> Schemas { get; set; }
}