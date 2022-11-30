using System.Linq;

namespace LSG.Infrastructure.DataServices.Queries
{
    public interface IBaseQueries<out T>
    {
        IQueryable<T> GetQuery();
    }
}