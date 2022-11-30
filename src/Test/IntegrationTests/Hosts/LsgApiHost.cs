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
using LSG.Hosts.LsgApi;
using LSG.SharedKernel.Extensions;
using NUnit.Framework;

namespace LSG.IntegrationTests.Hosts;

[TestFixture]
[Category(Const.TestCategory.Factory)]
public class LsgApiHost : HostBase<LsgApiStartup>
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


        var res = await client.GetAsync(LsgConfig.LsgApiUrl);

        var content = await res.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public override async Task CanGetApiStatus()
    {
        var client = HttpClientFactory.CreateClient();


        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new UriBuilder(LsgConfig.LsgApiUrl)
            {
                Path = "api/status",
                Query = $"key={Const.StatusKey}"
            }.Uri
        };
        try
        {
            var res = await client.SendAsync(request);
            var content = await res.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var response = JsonSerializer.Deserialize<AppServerStatus>(content);
            response.ServerInfos.First(a=>a.ServerType == ServerInfoType.Database.ToString()).IsConnected.Should().BeTrue();
            response.Site.Should().Be(Const.Sites.LsgApi);
            response.Site.Should().Be(CurrentSite);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public override async Task CanGetHealth()
    {
        var client = HttpClientFactory.CreateClient();


        var res = await client.GetAsync($"{LsgConfig.LsgApiUrl.AbsoluteUri.ToUrl("health")}");

        var content = await res.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}