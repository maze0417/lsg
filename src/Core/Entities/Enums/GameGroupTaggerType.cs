using System;

 namespace LSG.Core.Entities.Enums
{
    [Flags]
    public enum GameGroupTaggerType
    {
        Elevated = 1 << 0,//1
        New = 1 << 1 //2
    }
}