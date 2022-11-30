using System;

namespace LSG.Core.Entities.Enums
{
    [Flags]
    public enum AnchorStatus
    {
        Enabled = 0,

        Disabled = 1 << 0, //1
    }
}