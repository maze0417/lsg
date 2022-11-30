using System.Linq;
using LSG.Core.Entities;

namespace LSG.Infrastructure.DataServices.Queries;

public interface ISchemaQueries : IBaseQueries<Schema>
{
}

public sealed class SchemaQueries : ISchemaQueries
{
    private readonly ILsgReadOnlyRepository _readOnlyRepository;

    public SchemaQueries(ILsgReadOnlyRepository readOnlyRepository)
    {
        _readOnlyRepository = readOnlyRepository;
    }


    IQueryable<Schema> IBaseQueries<Schema>.GetQuery()
    {
        return _readOnlyRepository.Schemas;
    }
}