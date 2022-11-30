using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace LSG.Infrastructure.DataServices;

public static class BaseRepositoryBulkExtensions
{
    public static Task BulkInsertAsync<T>(
        this IBaseRepository repository,
        IList<T> items)
        where T : class
    {
        if (items.IsNullOrEmpty()) return Task.CompletedTask;

        var repo = (DbContext)repository;

        return repo.BulkInsertAsync(items);
    }

    public static void BulkInsert<T>(
        this IBaseRepository repository,
        IList<T> items)
        where T : class
    {
        if (items.IsNullOrEmpty()) return;

        var repo = (DbContext)repository;

        repo.BulkInsert(items);
    }

    public static Task BulkUpdateAsync<T>(
        this IBaseRepository repository,
        IList<T> items,
        params string[] columnToUpdate)
        where T : class
    {
        if (items.IsNullOrEmpty()) return Task.CompletedTask;

        var repo = (DbContext)repository;
        return repo.BulkUpdateAsync(items,
            config => { config.PropertiesToInclude = columnToUpdate.ToList(); });
    }

    private static bool IsNullOrEmpty<T>(this IEnumerable<T> items)
    {
        return !(items?.Any() ?? false);
    }
}