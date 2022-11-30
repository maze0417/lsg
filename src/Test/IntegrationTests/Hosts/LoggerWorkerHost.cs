using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LSG.Core;
using LSG.Core.Messages;
using LSG.Hosts.LoggerWorker;
using LSG.SharedKernel.Extensions;
using NUnit.Framework;

namespace LSG.IntegrationTests.Hosts
{
    [TestFixture]
    public class LoggerWorkerHost : HostBase<LoggerWorkerStartup>
    {
        [Test]
        public override Task CanGetAllRegisteredTypes()
        {
            ResolveRegistrationsAndControllers();
            return Task.CompletedTask;
        }

        [Test]
        public override async Task CanGetRootStatus()
        {
            var client = HttpClientFactory.CreateClient();


            var res = await client.GetAsync(LsgConfig.LoggerWorkerURl);

            var content = await res.Content.ReadAsStringAsync();

            Console.WriteLine(content);
            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public override async Task CanGetApiStatus()
        {
            var client = HttpClientFactory.CreateClient();


            var res = await client.GetAsync($"{LsgConfig.LoggerWorkerURl}api/status?key={Const.StatusKey}");

            var content = await res.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var response = JsonSerializer.Deserialize<ServerStatus>(content);
            response.Site.Should().Be(Const.Sites.LoggerWorker);

            response.Site.Should().Be(CurrentSite);
        }

        [Test]
        public override async Task CanGetHealth()
        {
            var client = HttpClientFactory.CreateClient();


            var res = await client.GetAsync($"{LsgConfig.LoggerWorkerURl.AbsoluteUri.ToUrl("health")}");

            var content = await res.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}