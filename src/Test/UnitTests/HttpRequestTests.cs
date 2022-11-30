using FluentAssertions;
using LSG.Core;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace LSG.UnitTests
{
    [TestFixture]
    public class HttpRequestTests
    {
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("iphone", true)]
        [TestCase("chrome", false)]
        public void CanDetermineMobileByHeader(string userAgent, bool isMobile)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Const.ForwardHeaders.UserAgent] = userAgent;


            httpContext.Request.IsMobile().Should().Be(isMobile);
        }

        [Test]
        public void IsNotMobileIfRequestHeaderIsNull()
        {
            var httpContext = new DefaultHttpContext();


            httpContext.Request.IsMobile().Should().Be(false);
        }
    }
}