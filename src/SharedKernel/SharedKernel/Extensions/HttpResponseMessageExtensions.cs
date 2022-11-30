using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LSG.SharedKernel.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task WriteToConsole(this HttpResponseMessage response, string requestContent)
        {
            Console.WriteLine($@"Path:{response.RequestMessage.RequestUri}");
            Console.WriteLine($@"Header:{response.RequestMessage.Headers}");
            Console.WriteLine($@"HttpStatus code:{response.StatusCode}");
            Console.WriteLine($@"Request content:{requestContent}");
            Console.WriteLine($@"Response content:{await response.Content.ReadAsStringAsync()}");
        }
    }
}