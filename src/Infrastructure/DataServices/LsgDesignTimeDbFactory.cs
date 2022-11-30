using LSG.Core;
using LSG.SharedKernel.AppConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LSG.Infrastructure.DataServices;

public sealed class LsgDesignTimeDbFactory : IDesignTimeDbContextFactory<LsgRepository>
{
    public LsgRepository CreateDbContext(string[] args)
    {
        var config = BaseAppConfig.GetConfiguration();
        var builder = new DbContextOptionsBuilder<LsgRepository>();

        builder
            .UseSqlServer(config.GetConnectionString(Const.ConnectionStringNames.Lsg));
        return new LsgRepository(builder.Options);
    }
}