using Renci.SshNet;

namespace Builds.Deployment.Tasks;

public interface IShellTask
{
    string ExecuteScript(string script);

    string ExecuteRemoteScript(string script);

    void CopyFileTo(string sourcePath, string server, string path);
    IShellTask EnterSession(string server);
    
    SshClient GetSshClient();
}