using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace LSG.Infrastructure.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void ApplyAllConfigurations(this ModelBuilder modelBuilder, Assembly resourceAssembly = null)
        {
            if (resourceAssembly == null)
                resourceAssembly = Assembly.GetCallingAssembly();

            var applyConfigurationMethodInfo = modelBuilder
                .GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(m => m.Name.Equals("ApplyConfiguration", StringComparison.OrdinalIgnoreCase));

            // https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7-1#inferred-tuple-element-names
            var ret = resourceAssembly
                .GetTypes()
                .Select(t => (t: t,
                    i: t.GetInterfaces().FirstOrDefault(i =>
                        i.Name.Equals(typeof(IEntityTypeConfiguration<>).Name, StringComparison.Ordinal))))
                .Where(it => it.i != null && !it.t.IsAbstract)
                .Select(it => (et: it.i.GetGenericArguments()[0], cfgObj: Activator.CreateInstance(it.t)))
                .Select(it =>
                    applyConfigurationMethodInfo.MakeGenericMethod(it.et).Invoke(modelBuilder, new[] {it.cfgObj}))
                .ToList();
        }

        public static void CascadeAllRelationsOnDelete(this ModelBuilder modelBuilder,
            DeleteBehavior behavior = DeleteBehavior.Restrict)
        {
            // This is the only way to do it right now is to iterate through all relationships
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = behavior;
            }
        }
    }
}