using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LSG.UnitTests
{
    [TestFixture]
    public class ServiceCollectionsTests
    {
        [Test]
        public void CanGetTheSameInstance()
        {
            var sc = new ServiceCollection();

            sc.AddSingleton<QueryA>();

            sc.AddSingleton<Func<string, IQuery>>(provider => s =>
            {
                if (s == "QueryA")
                    return provider.GetRequiredService<QueryA>();
                return null;
            });

            var providers = sc.BuildServiceProvider();

            var func = providers.GetRequiredService<Func<string, IQuery>>();
            var query1 = func("QueryA");
            var query2 = func("QueryA");
            query1.GetHashCode().Should().Be(query2.GetHashCode());
        }

        [Test]
        public void CanNotGetTheSameInstanceWhenReturnNew()
        {
            var sc = new ServiceCollection();

            sc.AddSingleton<Func<string, IQuery>>(provider => s =>
            {
                if (s == "QueryA")
                    return new QueryA();
                return null;
            });

            var providers = sc.BuildServiceProvider();

            var func = providers.GetRequiredService<Func<string, IQuery>>();
            var query1 = func("QueryA");
            var query2 = func("QueryA");
            query1.GetHashCode().Should().NotBe(query2.GetHashCode());
        }

        public interface IQuery
        {
            string GetData();
        }

        public class QueryA : IQuery
        {
            public string GetData()
            {
                return "ok";
            }
        }
    }
}