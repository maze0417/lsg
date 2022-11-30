using System.ComponentModel;

 namespace LSG.Core.Entities.Enums
{
    public enum MaintenanceType
    {
        [Description("Brand")]
        Brand = 0,
        [Description("Game provider")]
        GameProvider = 1,
        [Description("Lobby")]
        Lobby = 2
    }
}