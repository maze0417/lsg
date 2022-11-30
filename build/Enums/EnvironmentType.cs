using System.ComponentModel;

namespace Builds.Deployment.Enums
{
    public enum EnvironmentType
    {
        [Description("Qa")] Qa,
        [Description("Staging")] Staging,
        [Description("Production")] Production,
        Integration,
        IntegrationTest,
        UnitTest,
    }
}