using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LSG.Core;
using LSG.Core.Enums;
using LSG.Core.Messages;
using LSG.Hosts.LsgFrontend;
using NUnit.Framework;

namespace LSG.IntegrationTests.Hosts
{
    [TestFixture, Category(Const.TestCategory.Factory)]
    public class LsgFrontendHost : HostBase<LsgFrontendStartup>
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
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new UriBuilder(LsgConfig.LsgFrontendUrl)
                {
                    Path = "api/status"
                }.Uri
            };

            try
            {
                var res = await client.SendAsync(request);
                var content = await res.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                res.StatusCode.Should().Be(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Test]
        public override async Task CanGetApiStatus()
        {
            var client = HttpClientFactory.CreateClient();


            var res = await client.GetAsync($"{LsgConfig.LsgFrontendUrl}api/status?key={Const.StatusKey}");

            var content = await res.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var response = JsonSerializer.Deserialize<FrontendServerStatus>(content);
            response.ServerInfos.First(a=>a.ServerType == ServerInfoType.Database.ToString()).IsConnected.Should().BeTrue();
            response.Site.Should().Be(Const.Sites.LsgFrontend);
            response.Site.Should().Be(CurrentSite);
        }


        [Test]
        public override async Task CanGetHealth()
        {
            var client = HttpClientFactory.CreateClient();


            var res = await client.GetAsync($"{LsgConfig.LsgFrontendUrl}health");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }

    
    }
}