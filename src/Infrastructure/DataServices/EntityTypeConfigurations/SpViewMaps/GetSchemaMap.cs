using LSG.Core.Messages.StoredProc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LSG.Infrastructure.DataServices.EntityTypeConfigurations.SpViewMaps;

public class GetSchemaMap : IEntityTypeConfiguration<GetSchema>
{
    public void Configure(EntityTypeBuilder<GetSchema> builder)
    {
        builder.HasNoKey().ToView(nameof(GetSchema));
    }
}