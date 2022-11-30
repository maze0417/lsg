namespace LSG.Core.Entities.Enums
{
    public enum TaskTriggeringStatus
    {
        Done = 0,
        ScheduledToDo = 1,
        LockedByOtherProcess = 2,
        SystemError = 3
    }
}