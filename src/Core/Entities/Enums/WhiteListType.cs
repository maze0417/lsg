using System;

 namespace LSG.Core.Entities.Enums
{
    [Flags]
    public enum WhitelistType : short
    {
        None = 0,
        Lobby = 1 << 0,
        BackOffice = 1 << 1,
        Api = 1 << 2
    }
}