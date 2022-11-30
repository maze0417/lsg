using System;
using System.Data;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Infrastructure.DataServices.DataInitializers;
using LSG.SharedKernel.Logger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LSG.Infrastructure.DataServices.Data
{
    public static class DataSeeder
    {
        public static Task SeedAsync(IServiceProvider serviceProvider)
        {
            var trans = serviceProvider.GetRequiredService<ITransactionManager>();

            var repoFactory = serviceProvider.GetRequiredService<Func<ILsgRepository>>();

            var logger = serviceProvider.GetRequiredService<ILsgLogger>();

            return trans.ExecuteTransactionAsync(
                repoFactory, IsolationLevel.ReadCommitted, nameof(DataSeeder),
                async (repository, transaction) =>
                {
                    logger.LogConsole(Const.SourceContext.DataSeeder, "Starting data seeding");
                    if (await repository.Schemas.AnyAsync())
                    {
                        logger.LogConsole(Const.SourceContext.DataSeeder, "Exist lang data , stopped seeding");

                        return false;
                    }

                    var seeder = new DevelopmentDataInitializer(repository);
                    seeder.SetInitialData();


                    logger.LogConsole(Const.SourceContext.DataSeeder, "Seeding is done");
                    return true;
                });
        }
    }
}