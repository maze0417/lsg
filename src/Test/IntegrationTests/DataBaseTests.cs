using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using LSG.Core.Messages.StoredProc;
using LSG.Hosts.LsgApi;
using LSG.Infrastructure;
using LSG.Infrastructure.DataServices;
using LSG.Infrastructure.Security;
using LSG.SharedKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LSG.IntegrationTests;

[TestFixture]
public class DataBaseTests : Base<LsgApiStartup>
{
    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public async Task CanQueryBySpFromDb()
    {
        using var db = CreateDb();
        const string sql =
            @"exec dbo.[sp_GetSchema] {0}";


        var name = "init";
        var players =
            await db.SqlQueryArrayAsync<GetSchema>(sql, name);

        Console.WriteLine(players.ToJson());
        players.Single().Name.Should().Be(name);
    }

    [Test]
    public void CanSeeDatabase()
    {
        using var db = CreateDb();
        var found = false;
        foreach (var s in db.Schemas)
        {
            found = true;
            Console.WriteLine(s.Name);
        }

        found.Should().Be(true);
    }


    [Test]
    [Ignore("to do when table added")]
    public void CanRetrieveManyToManyJoinedValue()
    {
    }

    [TestCase(false)]
    public void CanGetReadOnlyAndReadWriteConnection(bool encryptDbString)
    {
        var config = DefaultFactory.GetService<ILsgConfig>();
        var crypto = DefaultFactory.GetService<ICryptoProvider>();


        var repo = DefaultFactory.GetService<ILsgRepository>();
        repo.GetConnectionString.Should()
            .Contain(encryptDbString
                ? Encoding.UTF8.GetString(crypto.DecryptBytes(config.LsgConnectionString))
                : config.LsgConnectionString);


        var readOnlyRepo = DefaultFactory.GetService<ILsgReadOnlyRepository>();
        readOnlyRepo.GetConnectionString.Should()
            .Contain(encryptDbString
                ? Encoding.UTF8.GetString(crypto.DecryptBytes(config.LsgReadOnlyConnectionString))
                : config.LsgConnectionString);

        Console.WriteLine($@"R/W :{repo.GetConnectionString}");
        Console.WriteLine($@"Read : {readOnlyRepo.GetConnectionString}");
    }

    [Test]
    public void CanConnectReadOnlyDb()
    {
        var readOnlyRepository = DefaultFactory.GetService<ILsgReadOnlyRepository>();
        readOnlyRepository.GetConnectionInfo().IsConnected.Should().BeTrue();
    }

    [Test]
    public void CanGetTheSameLsgRepo()
    {
        var repo1 = DefaultFactory.GetRequiredService<ILsgRepository>();
        var repo2 = DefaultFactory.GetRequiredService<ILsgRepository>();


        repo1.GetHashCode().Should().Be(repo2.GetHashCode());
    }

    [Test]
    public void CanGetTheDiffLsgRepo()
    {
        var repo1 = DefaultFactory.CreateScope().ServiceProvider.GetRequiredService<ILsgRepository>();
        var repo2 = DefaultFactory.CreateScope().ServiceProvider.GetRequiredService<ILsgRepository>();


        repo1.GetHashCode().Should().NotBe(repo2.GetHashCode());
    }

    [Test]
    public void CanGetTheDiffFuncLsgRepo()
    {
        var repo = DefaultFactory.GetRequiredService<Func<ILsgRepository>>();


        repo().GetHashCode().Should().NotBe(repo().GetHashCode());
    }

    private ILsgRepository CreateDb()
    {
        return DefaultFactory.GetService<ILsgRepository>();
    }
}