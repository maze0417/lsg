using System;
using System.ComponentModel;

namespace LSG.Core.Enums
{
    [Flags]
    public enum PlayerType
    {
        None = 0,
        [Description("Real Player")] RealPlayer = 1,
        [Description("Anchor Player")] AnchorPlayer = 2,
        [Description("Test Player")] TestPlayer = 4,
    }
}