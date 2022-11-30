using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LSG.Core.Entities;
using LSG.SharedKernel.Extensions;

namespace LSG.Infrastructure.DataServices.DataInitializers;

internal sealed class DevelopmentDataInitializer
{
    private static readonly object Locker = new();
    private readonly StringBuilder _logBuilder = new();
    private readonly ILsgRepository _repository;

    internal DevelopmentDataInitializer(ILsgRepository repo)
    {
        _repository = repo;
    }

    internal void SetInitialData()
    {
        if (_repository.Schemas.Any()) return;

        var timer = new Stopwatch();
        timer.Start();


        LogSeedingTime(GenerateSchemaData, _logBuilder);

        timer.Stop();
        _logBuilder.AppendLine($"Total Time was {timer.ElapsedMilliseconds} ms");
        Console.WriteLine(_logBuilder.ToString());
    }

    private static void LogSeedingTime(Action getFunc,
        StringBuilder builder)
    {
        var timer = Stopwatch.StartNew();
        getFunc();
        timer.Stop();
        lock (Locker)
        {
            builder.AppendLine($"{getFunc.Method.Name} total spent {timer.ElapsedMilliseconds} ms");
        }
    }


    private void GenerateSchemaData()
    {
        var data = new[]
        {
            new Schema
            {
                Name = "init",
                CreatedOn = DateTime.Now,
                Version = DateTime.UtcNow.ToUnixTimeSeconds()
            },
            new Schema
            {
                Name = "sec",
                CreatedOn = DateTime.Now,
                Version = DateTime.UtcNow.ToUnixTimeSeconds() + 1
            }
        };

        _repository.BulkInsert(data);
    }
}