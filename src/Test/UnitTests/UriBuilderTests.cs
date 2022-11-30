using System;
using FluentAssertions;
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Fixtures)]

namespace LSG.UnitTests
{
    [TestFixture]
    public class UriBuilderTests
    {
        [TestCase("http://my.com", "api/fish")]
        [TestCase("http://my.com///", "/api/fish")]
        [TestCase("http://my.com/", "/api/fish")]
        public void CanGetUrl(string server, string path)
        {
            var url = new UriBuilder(server)
            {
                Path = path
            }.Uri.AbsoluteUri;
            url.Should().Be("http://my.com/api/fish");
        }

        [TestCase("http://my.com", "api/fish", "token=123", "http://my.com/api/fish?token=123")]
        [TestCase("http://my.com", "api/fish", null, "http://my.com/api/fish")]
        [TestCase("http://my.com", "api/fish", "", "http://my.com/api/fish")]
        public void CanGetUrlWithQueryString(string server, string path, string query, string expected)
        {
            var url = new UriBuilder(server)
            {
                Path = path,
                Query = query
            }.Uri.AbsoluteUri;
            url.Should().Be(expected);
        }

        [TestCase(
            "http://uat.aggdemo.com:81/ZB/Pc/index.jsp?username=HT002&pid=TST&nick=HT002&userFlag=0&lang=hans&token=05a6b865f8cbd9ec1dec62857c051b89&allow=YP_SLOT_HUNTER&tips=true&src_platform=LIVE&ipdomains=138.43.195.18&ips=138.43.195.18")]
        public void CanGetHost(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                Assert.Fail($"faile to parse url: {url}");
            }

            var newOne = new Uri($"{uri.Scheme}://{uri.Authority}");

            newOne.AbsoluteUri.Should().Be("http://uat.aggdemo.com:81/");
        }
    }
}