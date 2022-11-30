using System.Threading.Tasks;
using LSG.Core.Entities;
using LSG.Core.Enums;
using LSG.Infrastructure.DataServices.Queries;
using LSG.SharedKernel.Redis;
using Microsoft.EntityFrameworkCore;

namespace LSG.Infrastructure.DataServices.Operations;

public interface ISchemaOperations
{
    Task<Schema[]> GetSchemasAsync();
}

public sealed class SchemaOperations : ISchemaOperations
{
    private readonly IRedisCacheManager _redisCacheManager;
    private readonly ISchemaQueries _schemaQueries;

    public SchemaOperations(IRedisCacheManager redisCacheManager, ISchemaQueries schemaQueries)
    {
        _redisCacheManager = redisCacheManager;
        _schemaQueries = schemaQueries;
    }

    Task<Schema[]> ISchemaOperations.GetSchemasAsync()
    {
        return _redisCacheManager.SafeStringGetAndCacheAsync(
            RedisCacheType.CacheSchema,
            RedisCacheType.CacheSchema,
            () =>
                _schemaQueries.GetQuery()
                    .ToArrayAsync()
        );
    }
}