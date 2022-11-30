using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LSG.Infrastructure.Extensions
{
    public static class MigrationBuilderExtensions
    {
        public static void SqlResource(this MigrationBuilder mb, string sqlResource, Assembly resourceAssembly = null,
            bool suppressTransaction = false)
        {
            if (resourceAssembly == null)
                resourceAssembly =
                    Assembly.GetCallingAssembly(); //should be here instead Getsql to get correct assembly
            mb.Sql(GetSql(sqlResource, resourceAssembly), suppressTransaction);
        }


        private static string GetSql(string sqlResource, Assembly resourceAssembly)
        {
            var availableResource = resourceAssembly.GetManifestResourceNames();
            if (availableResource.All(c => !c.Equals(sqlResource)))
                throw new ArgumentException($"UnableToLoadEmbeddedResource {resourceAssembly.FullName} {sqlResource}");

            using var stream = resourceAssembly.GetManifestResourceStream(sqlResource);
            if (stream == null)
                throw new ArgumentException(
                    $"UnableToLoadEmbeddedResource Stream {resourceAssembly.FullName} {sqlResource}");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}