using System;
using System.Data;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using LSG.Core;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.EntityFrameworkCore.Storage;

namespace LSG.Infrastructure.DataServices
{
    public interface ITransactionManager
    {
        Task<TResult> ExecuteTransactionAsync<TContext, TResult>(
            Func<TContext> contextFactory,
            IsolationLevel level,
            string externalTransactionId,
            Func<TContext, IDbContextTransaction, Task<TResult>> execute,
            Func<TContext, TResult, Task> onPostCommit = null,
            Func<TContext, TResult, Exception, Task> onPostRollback = null)
            where TContext : IBaseRepository;
    }

    public sealed class TransactionManager : ITransactionManager
    {
        private readonly ILsgLogger _lsgLogger;

        public TransactionManager(ILsgLogger lsgLogger)
        {
            _lsgLogger = lsgLogger;
        }


        async Task<TResult> ITransactionManager.ExecuteTransactionAsync<TContext, TResult>(
            Func<TContext> contextFactory,
            IsolationLevel level,
            string externalTransactionId,
            Func<TContext, IDbContextTransaction, Task<TResult>> execute,
            Func<TContext, TResult, Task> onPostCommit,
            Func<TContext, TResult, Exception, Task> onPostRollback)
        {
            using (var context = contextFactory())
            using (var transaction = context.BeginTransaction(level))
            {
                ExceptionDispatchInfo errorInfo;
                TResult result = default;
                try
                {
                    result = await execute(context, transaction);

                    await transaction.CommitAsync();

                    try
                    {
                        if (onPostCommit != null)
                            await onPostCommit(context, result);
                    }
                    catch (Exception ex)
                    {
                        _lsgLogger.LogError(Const.SourceContext.TransactionManager, ex,
                            "Error during post transaction committed.");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    errorInfo = ExceptionDispatchInfo.Capture(ex);
                }


                _lsgLogger.LogWarning(Const.SourceContext.TransactionManager,
                    $"Error during transaction Id:'{externalTransactionId}' {errorInfo.SourceException.GetMessageChain()}",
                    errorInfo);

                try
                {
                    if (transaction.GetDbTransaction().Connection != null)
                    {
                        await transaction.RollbackAsync();
                    }
                }
                catch (Exception ex2)
                {
                    _lsgLogger.LogWarning(Const.SourceContext.TransactionManager,
                        $"Error during rolling back transaction with External Id:'{externalTransactionId}' {errorInfo.SourceException.GetMessageChain()}",
                        ex2);
                }

                if (onPostRollback != null)
                {
                    await onPostRollback(context, result, errorInfo.SourceException);
                }

                errorInfo.Throw();
            } // here we dispose the transaction and connection

            throw new InvalidOperationException("This part of the code should never be reached");
        }
    }
}