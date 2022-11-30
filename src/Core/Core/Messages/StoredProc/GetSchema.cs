using System;

namespace LSG.Core.Messages.StoredProc;

public class GetSchema
{
    public long Version { get; set; }
    public string Name { get; set; }
    public DateTime CreatedOn { get; set; }
}