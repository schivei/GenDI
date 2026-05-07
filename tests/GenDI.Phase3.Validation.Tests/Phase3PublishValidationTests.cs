using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace GenDI.Phase3.Validation.Tests;

public class Phase3PublishValidationTests
{
    [Fact]
    public void Trim_publish_succeeds()
    {
        var projectPath = GetProjectPath(
            "GenDI.Phase3.TrimValidation.App/GenDI.Phase3.TrimValidation.App.csproj"
        );
        RunDotnetPublish(projectPath, "-c Release");
    }

    [Fact]
    public void NativeAot_publish_succeeds()
    {
        var projectPath = GetProjectPath(
            "GenDI.Phase3.NativeAotValidation.App/GenDI.Phase3.NativeAotValidation.App.csproj"
        );
        RunDotnetPublish(projectPath, $"-c Release -r {GetCurrentRuntimeIdentifier()}");
    }

    private static string GetProjectPath(string relativeProjectPath)
    {
        var root = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(root, "GenDI.slnx")))
        {
            var parent = Directory.GetParent(root);
            if (parent is null)
            {
                throw new DirectoryNotFoundException(
                    "Could not locate repository root containing GenDI.slnx."
                );
            }

            root = parent.FullName;
        }

        return Path.Combine(root, "tests", relativeProjectPath);
    }

    private static void RunDotnetPublish(string projectPath, string extraArguments)
    {
        var arguments = $"publish \"{projectPath}\" {extraArguments} --nologo";
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            $"dotnet {arguments} failed with exit code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{output}{Environment.NewLine}STDERR:{Environment.NewLine}{error}"
        );
    }

    private static string GetCurrentRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-x64";
        }

        throw new PlatformNotSupportedException(
            "Unsupported platform for NativeAOT publish validation."
        );
    }
}
