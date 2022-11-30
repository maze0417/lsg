using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Builds.Deployment.Extensions;
using Nuke.Common;
using Nuke.Common.Utilities;
using Renci.SshNet;

namespace Builds.Deployment.Tasks;

public partial class SshShellTasks : IShellTask
{
    private static readonly HashSet<string> IgnoreErrorString =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Error response from daemon: a prune operation is already running",
            "Removing service",
            "Removing network",
            "Errors, Failures and Warnings",
            "Access to the path",
            "Cannot remove item",
            "WARNING! Using --password via the CLI is insecure. Use --password-stdin.",
            "Test Run Failed"
        };

    private static readonly HashSet<string> ForceErrorString =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "failed to build"
        };

    private static readonly HashSet<string> ForceWarningCommand =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "docker-compose",
            "docker login",
            "docker image rm"
        };

    private readonly PrivateKeyFile _devKeyFile;
    private readonly string _pwd;
    private readonly string _userName;
    private readonly string _workerDir;
    private SshClient _remoteClient;

    public SshShellTasks(string userName, string pwd, string workerDir)
    {
        _userName = userName;
        _workerDir = workerDir;
        _pwd = pwd;
        _devKeyFile = new PrivateKeyFile($"{workerDir}/build/ConfigFiles/dev/soga-dev.pem");
    }

    public string ExecuteScript(string script)
    {
        Logger.Info($"Run>>> {script}");
        var isForceWarningCommand =
            ForceWarningCommand.Any(script.Contains);


        using var process = new Process
        {
            StartInfo =
            {
                FileName = "/bin/bash",
                Arguments = $@"-c ""{script}""",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = _workerDir
            }
        };


        var response = new List<string>();
        process.OutputDataReceived += (_, args) => { UpdateText(args.Data, response); };
        process.ErrorDataReceived += (_, args) => { UpdateErrorText(args.Data, response, isForceWarningCommand); };
        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        catch (Exception e)
        {
            Logger.Error($"error when execute -> {e}");
            throw;
        }


        return response.ToArray().JoinNewLine();
    }

    public string ExecuteRemoteScript(string script)
    {
        if (_remoteClient == null) throw new Exception("need to call enter session first");
        script = "sudo " + script;
        var log = $"Run>>> {script} ({_remoteClient.ConnectionInfo.Host})";
        Logger.Info(log);


        var res = CreateCommand(script, _remoteClient, log);

        return res;
    }

    public void CopyFileTo(string sourcePath, string server, string path)
    {
        Logger.Info($"Run>>>Copy from {sourcePath} to {server}:{path}");

        using (var client = new SftpClient(server, _userName, _devKeyFile))
        {
            client.Connect();
            using (var sourceStream = File.OpenRead(sourcePath))
            using (var destStream = client.Create(path))
            {
                sourceStream.CopyTo(destStream);
            }
        }
    }

    public IShellTask EnterSession(string server)
    {
        var client = new SshClient(server, _userName, _devKeyFile);
        client.Connect();
        Logger.Info($"Connection to {client.ConnectionInfo.Host} is ok.");

        _remoteClient = client;
        return this;
    }

    public SshClient GetSshClient()
    {
        return _remoteClient;
    }

    public string ExecuteScript(string script, string server)
    {
        var log = $"Run>>> {script} ({server})";
        Logger.Info(log);

        using var sshClient = new SshClient(server, _userName, _pwd);
        sshClient.Connect();

        var res = CreateCommand(script, sshClient, log);
        sshClient.Disconnect();
        return res;
    }

    public string ExecuteScript(string script, SshClient client)
    {
        var log = $"Run>>> {script} ({client.ConnectionInfo.Host})";
        Logger.Info(log);
        return CreateCommand(script, client, log);
    }

    private static string CreateCommand(string script, SshClient sshClient, string log)
    {
        using var sshCmd = sshClient.CreateCommand(script);
        var response = new List<string>();
        var isForceWarningCommand =
            ForceWarningCommand.Any(script.Contains);

        var outputs = new Progress<ScriptOutputLine>(obj =>
        {
            if (obj.IsErrorLine)
            {
                UpdateErrorText(obj.Line, response, isForceWarningCommand);
                return;
            }

            UpdateText(obj.Line, response);
        });


        try
        {
            sshCmd.ExecuteAsync(outputs, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            throw new Exception($"exception when execute -> {log} => ex :{ex}");
        }

        if (sshCmd.ExitStatus != 0)
            throw new Exception(
                $"Ssh ExitStatus:{sshCmd.ExitStatus}, Error:{sshCmd.Error ?? sshCmd.Result} , Script Stopped => {log}");

        var res = sshCmd.Result.Replace("\n", Environment.NewLine);

        return res;
    }


    private static void UpdateErrorText(string line, ICollection<string> response, bool warningErrorByCommand)
    {
        if (string.IsNullOrEmpty(line))
            return;
        response.Add(line);

        if (ForceErrorString.Any(line.Contains))
        {
            Logger.Error(line);
            throw new Exception($"Found error output {line}, Script terminate");
        }

        if (warningErrorByCommand || IgnoreErrorString.Any(line.Contains))
        {
            Logger.Warn(line);
            return;
        }

        Logger.Error(line);
        throw new Exception("Found error output , Script Stopped");
    }

    private static void UpdateText(string line, ICollection<string> response)
    {
        if (string.IsNullOrEmpty(line))
            return;
        response.Add(line);
        if (IgnoreErrorString.Any(a => line.Contains(a)))
        {
            Logger.Info(line);
            return;
        }

        var hasError =
            line.StartsWith("error", StringComparison.InvariantCultureIgnoreCase) ||
            line.StartsWith("Info: ERROR:", StringComparison.InvariantCultureIgnoreCase) ||
            line.IndexOf(": error CS", StringComparison.InvariantCultureIgnoreCase) > 0 ||
            line.IndexOf(": error MS", StringComparison.InvariantCultureIgnoreCase) > 0 ||
            line.IndexOf("terminating abnormally", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            line.IndexOf("Incorrect syntax", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            line.IndexOf(": error :", StringComparison.InvariantCultureIgnoreCase) > 0;


        if (hasError || ForceErrorString.Any(line.Contains))
        {
            Logger.Error(line);
            throw new Exception("Found out data contain error message , Script Stopped");
        }

        Logger.Info(line);
    }
}