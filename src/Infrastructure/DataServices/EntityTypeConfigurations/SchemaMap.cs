using System;
using LSG.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LSG.Infrastructure.DataServices.EntityTypeConfigurations
{
    internal sealed class SchemaMap : EntityTypeConfiguration<Schema>
    {
        protected override void CustomConfigure(EntityTypeBuilder<Schema> builder)
        {
            builder.ToTable("Schemas");
            builder.HasKey(e => e.Version);
            builder.Property(e => e.Version)
                .ValueGeneratedNever();
            builder.Property(e => e.Name)
                .HasMaxLength(255);
            
            builder.Property(x=>x.CreatedOn);
        }
    }
}