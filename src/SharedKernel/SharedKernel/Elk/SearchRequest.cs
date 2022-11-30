using System.Collections.Generic;

namespace LSG.SharedKernel.Elk
{
    public class SearchRequest
    {
        public string Index { get; set; }
        public Dictionary<string, object> Query { get; set; }
    }
}