using System;

namespace Builds.Deployment.Enums;

[Flags]
public enum ActionType
{
    None = 0,
    DotnetBuild = 1 << 0,
    BuildImage = 1 << 1,
    RunContainer = 1 << 2,
    PushImage = 1 << 3,
    UnitTest = 1 << 4,
    IntegrationTest = 1 << 5,
    PullIImage = 1 << 6,
    CheckSite = 1 << 8,
    ExecuteDbMigration = 1 << 9,
    RunIntegration = BuildImage | PushImage | PullIImage | ExecuteDbMigration | RunContainer | CheckSite,
    OnlyDeployIntegration = ExecuteDbMigration | RunContainer | CheckSite,
    RunUnitTests = DotnetBuild | UnitTest,
    RunIntegrationTests = BuildImage | ExecuteDbMigration | RunContainer | CheckSite | IntegrationTest,
    DeployStagingOrProductionImage = BuildImage | PushImage
}