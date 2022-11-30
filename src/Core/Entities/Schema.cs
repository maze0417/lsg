using System;

 namespace LSG.Core.Entities
{
    public class Schema
    {
        public long Version { get; set; }
        public string Name { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}