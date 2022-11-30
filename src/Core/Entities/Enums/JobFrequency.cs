using System;

 namespace LSG.Core.Entities.Enums
{
    [Flags]
    public enum JobFrequency
    {
        Daily = 1 << 1,//2
        Weekly = 1 << 2,//4
        TwiceAMonth = 1 << 3,//8
        Monthly = 1 << 4,//16
        AnyTime = int.MaxValue
    }
}