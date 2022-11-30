using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LSG.Infrastructure.DataServices.EntityTypeConfigurations
{
    public abstract class EntityTypeConfiguration<T> : IEntityTypeConfiguration<T> where T : class
    {
        protected abstract void CustomConfigure(EntityTypeBuilder<T> builder);


        public void Configure(EntityTypeBuilder<T> builder)
        {
            CustomConfigure(builder);
        }
    }
}