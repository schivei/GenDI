using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace GenDI.Phase6.PlatformValidation.Tests;

public class Phase6PlatformValidationTests
{
    [Fact]
    public void MinimalApi_publish_succeeds()
    {
        var projectPath = GetProjectPath(
            "GenDI.Phase6.MinimalApiValidation.App/GenDI.Phase6.MinimalApiValidation.App.csproj"
        );

        RunDotnetCommand($"publish \"{projectPath}\" -c Release --nologo");
    }

    [Fact]
    public void WorkerService_publish_succeeds()
    {
        var projectPath = GetProjectPath(
            "GenDI.Phase6.WorkerValidation.App/GenDI.Phase6.WorkerValidation.App.csproj"
        );

        RunDotnetCommand($"publish \"{projectPath}\" -c Release --nologo");
    }

    [Fact]
    public void BlazorWasm_publish_succeeds()
    {
        var projectPath = GetProjectPath(
            "GenDI.Phase6.BlazorWasmValidation.App/GenDI.Phase6.BlazorWasmValidation.App.csproj"
        );

        RunDotnetCommand($"publish \"{projectPath}\" -c Release --nologo");
    }

    [Fact]
    public void FSharp_projects_do_not_receive_generated_AddGenDIServices_extension()
    {
        var root = GetRepositoryRoot();
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "gendi-phase6-fsharp",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(tempRoot);

        try
        {
            RunDotnetCommand($"new web -lang F# -n FSharpMinimal --force", tempRoot);

            var projectDirectory = Path.Combine(tempRoot, "FSharpMinimal");
            var projectPath = Path.Combine(projectDirectory, "FSharpMinimal.fsproj");
            var programPath = Path.Combine(projectDirectory, "Program.fs");

            var fsproj = File.ReadAllText(projectPath);
            fsproj = fsproj.Replace(
                "</Project>",
                $"""
                
                  <ItemGroup>
                    <ProjectReference Include="{Path.Combine(root, "src", "GenDI", "GenDI.csproj")}" />
                    <ProjectReference Include="{Path.Combine(root, "src", "GenDI.SourceGenerator", "GenDI.SourceGenerator.csproj")}" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
                  </ItemGroup>
                </Project>
                """
            );
            File.WriteAllText(projectPath, fsproj);

            File.WriteAllText(
                programPath,
                """
                open System
                open GenDI
                open Microsoft.AspNetCore.Builder
                open Microsoft.Extensions.DependencyInjection

                [<ServiceInjection>]
                type IClock =
                    abstract member UtcNow : DateTimeOffset

                [<Injectable(ServiceLifetime.Singleton)>]
                type SystemClock() =
                    interface IClock with
                        member _.UtcNow = DateTimeOffset.UtcNow

                let args : string array = [||]
                let builder = WebApplication.CreateBuilder(args)
                builder.Services.AddGenDIServices() |> ignore
                let app = builder.Build()
                app.MapGet("/", Func<IClock, string>(fun clock -> clock.UtcNow.ToString("O"))) |> ignore
                app.Run()
                """
            );

            var failure = RunDotnetCommand(
                $"build \"{projectPath}\" -nologo",
                tempRoot,
                expectSuccess: false
            );

            Assert.Contains("AddGenDIServices", failure, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static string GetProjectPath(string relativeProjectPath) =>
        Path.Combine(GetRepositoryRoot(), "tests", relativeProjectPath);

    private static string GetRepositoryRoot()
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

        return root;
    }

    private static string RunDotnetCommand(
        string arguments,
        string? workingDirectory = null,
        bool expectSuccess = true
    )
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory ?? GetRepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var combined = $"{output}{Environment.NewLine}{error}";

        if (expectSuccess)
        {
            Assert.True(
                process.ExitCode == 0,
                $"dotnet {arguments} failed with exit code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{output}{Environment.NewLine}STDERR:{Environment.NewLine}{error}"
            );
        }
        else
        {
            Assert.NotEqual(0, process.ExitCode);
        }

        return combined;
    }
}
