using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;

namespace Builds.Deployment.Extensions;

public static class SshCommandExtensions
{
    public static async Task ExecuteAsync(
        this SshCommand sshCommand,
        IProgress<ScriptOutputLine> progress,
        CancellationToken cancellationToken)
    {
        var asyncResult = sshCommand.BeginExecute();
        var stdoutReader = new StreamReader(sshCommand.OutputStream);
        var stderrReader = new StreamReader(sshCommand.ExtendedOutputStream);

        var stderrTask =
            CheckOutputAndReportProgressAsync(sshCommand, asyncResult, stderrReader, progress, true, cancellationToken);
        var stdoutTask = CheckOutputAndReportProgressAsync(sshCommand, asyncResult, stdoutReader, progress, false,
            cancellationToken);

        await Task.WhenAll(stderrTask, stdoutTask);

        sshCommand.EndExecute(asyncResult);
    }

    private static async Task CheckOutputAndReportProgressAsync(
        SshCommand sshCommand,
        IAsyncResult asyncResult,
        StreamReader streamReader,
        IProgress<ScriptOutputLine> progress,
        bool isError,
        CancellationToken cancellationToken)
    {
        while (!asyncResult.IsCompleted || !streamReader.EndOfStream)
        {
            if (cancellationToken.IsCancellationRequested) sshCommand.CancelAsync();

            cancellationToken.ThrowIfCancellationRequested();

            var stderrLine = await streamReader.ReadLineAsync();

            if (!string.IsNullOrEmpty(stderrLine))
                progress.Report(new ScriptOutputLine(
                    stderrLine,
                    isError));

            // wait 10 ms
            await Task.Delay(10, cancellationToken);
        }
    }
}

public class ScriptOutputLine
{
    public ScriptOutputLine(string line, bool isErrorLine)
    {
        Line = line;
        IsErrorLine = isErrorLine;
    }

    public string Line { get; }

    public bool IsErrorLine { get; }
}