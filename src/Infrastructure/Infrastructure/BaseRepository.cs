using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using LSG.Core.Messages;
using LSG.SharedKernel.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace LSG.Infrastructure;

public interface IBaseRepository : IDisposable
{
    bool HasChanges { get; }

    string GetConnectionString { get; }

    Task<int> SaveChangesAsync();

    int SaveChanges();

    EntityEntry<T> Entry<T>(T entity) where T : class;

    IDbContextTransaction BeginTransaction(IsolationLevel isolation);

    Task<int> ExecuteSqlCommandAsync(string sql, params object[] args);

    int ExecuteSqlCommand(string sql, params object[] args);

    Task<TEntity[]> SqlQueryArrayAsync<TEntity>(string sql, params object[] parameters)
        where TEntity : class;

    Task<TEntity> SqlQueryAsync<TEntity>(string sql, params object[] parameters)
        where TEntity : class;

    void MigrateToLatestVersion();

    DatabaseInfo GetConnectionInfo();
}

public abstract class BaseRepository : DbContext, IBaseRepository
{
    public BaseRepository(DbContextOptions options) : base(options)
    {
    }


    bool IBaseRepository.HasChanges => ChangeTracker.HasChanges();

    Task<int> IBaseRepository.SaveChangesAsync()
    {
        return SaveChangesAsync();
    }

    EntityEntry<T> IBaseRepository.Entry<T>(T entity)
    {
        return base.Entry(entity);
    }


    IDbContextTransaction IBaseRepository.BeginTransaction(IsolationLevel isolation)
    {
        return Database.BeginTransaction(isolation);
    }

    Task<int> IBaseRepository.ExecuteSqlCommandAsync(string sql, params object[] args)
    {
        return Database.ExecuteSqlRawAsync(sql, args);
    }

    public int ExecuteSqlCommand(string sql, params object[] args)
    {
        return Database.ExecuteSqlRaw(sql, args);
    }

    Task<TEntity[]> IBaseRepository.SqlQueryArrayAsync<TEntity>(string sql, params object[] parameters)
    {
        return Set<TEntity>().FromSqlRaw(sql, parameters).ToArrayAsync();
    }

    Task<TEntity> IBaseRepository.SqlQueryAsync<TEntity>(string sql, params object[] parameters)
    {
        return Set<TEntity>().FromSqlRaw(sql, parameters).FirstOrDefaultAsync();
    }

    void IBaseRepository.MigrateToLatestVersion()
    {
        Database.Migrate();
    }

    DatabaseInfo IBaseRepository.GetConnectionInfo()
    {
        var conn = Database.GetDbConnection();
        
        try
        {
            conn.Open();
            conn.Close();
            return new DatabaseInfo
            {
                IsConnected = Database.CanConnect(),
                Message = "ok",
                Host = conn.DataSource,
                Name = conn.Database
            };
        }
        catch (Exception ex)
        {
            return new DatabaseInfo
            {
                IsConnected = Database.CanConnect(),
                Message = ex.GetMessageChain(),
                Host = conn?.DataSource,
                Name = conn?.Database
            };
        }
    }

    string IBaseRepository.GetConnectionString => Database.GetDbConnection().ConnectionString;
}