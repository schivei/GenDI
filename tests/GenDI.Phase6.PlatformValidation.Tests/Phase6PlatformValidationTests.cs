using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GenDI.Phase6.PlatformValidation.Tests;

public class Phase6PlatformValidationTests
{
    private const string SkipFSharpTemplateTestEnvironmentVariable =
        "GENDI_SKIP_FSHARP_TEMPLATE_TEST";

    private static CancellationToken Token
    {
        get
        {
            var source = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token, TestContext.Current.CancellationToken).Token.Register(() =>
            {
                Assert.Fail(
                    $"Test timed out after 30 seconds. This may be due to an issue with the .NET SDK installation or environment configuration.{Environment.NewLine}Ensure that the .NET SDK is correctly installed and that F# templates are available."
                );
            });

            return source.Token;
        }
    }

    [Fact]
    public Task MinimalApi_publish_succeeds()
    {
        var projectPath = GetProjectPath(
            "GenDI.Phase6.MinimalApiValidation.App/GenDI.Phase6.MinimalApiValidation.App.csproj"
        );

        return RunDotnetCommandAsync($"publish \"{projectPath}\" -c Release --nologo", cancellationToken: Token);
    }

    [Fact]
    public Task WorkerService_publish_succeeds()
    {
        var projectPath = GetProjectPath(
            "GenDI.Phase6.WorkerValidation.App/GenDI.Phase6.WorkerValidation.App.csproj"
        );

        return RunDotnetCommandAsync($"publish \"{projectPath}\" -c Release --nologo", cancellationToken: Token);
    }

    [Fact]
    public Task BlazorWasm_publish_succeeds()
    {
        var projectPath = GetProjectPath(
            "GenDI.Phase6.BlazorWasmValidation.App/GenDI.Phase6.BlazorWasmValidation.App.csproj"
        );

        return RunDotnetCommandAsync($"publish \"{projectPath}\" -c Release --nologo", cancellationToken: Token);
    }

    [Fact]
    [Trait("Category", "TemplateDependent")]
    public async Task FSharp_projects_do_not_receive_generated_AddGenDIServices_extension()
    {
        if (
            string.Equals(
                Environment.GetEnvironmentVariable(SkipFSharpTemplateTestEnvironmentVariable),
                "1",
                StringComparison.Ordinal
            )
        )
        {
            Assert.Skip(
                $"Set {SkipFSharpTemplateTestEnvironmentVariable}=0 to run the F# template validation."
            );
        }

        var root = GetRepositoryRoot();
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "gendi-phase6-fsharp",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(tempRoot);

        try
        {
            var (ExitCode, Output) = await TryRunDotnetCommandAsync(
                $"new web -lang F# -n FSharpMinimal --force",
                tempRoot,
                Token
            );
            if (ExitCode != 0)
            {
                Assert.Skip(
                    $"F# ASP.NET Core templates are unavailable in this environment.{Environment.NewLine}{Output}"
                );
            }

            var projectDirectory = Path.Combine(tempRoot, "FSharpMinimal");
            var projectPath = Path.Combine(projectDirectory, "FSharpMinimal.fsproj");
            var programPath = Path.Combine(projectDirectory, "Program.fs");

            var fsproj = await File.ReadAllTextAsync(projectPath, Token);
            fsproj = fsproj.Replace(
                "</Project>",
                $"""

                  <ItemGroup>
                    <ProjectReference Include="{Path.Combine(
                    root,
                    "src",
                    "GenDI",
                    "GenDI.csproj"
                )}" />
                    <ProjectReference Include="{Path.Combine(
                    root,
                    "src",
                    "GenDI.SourceGenerator",
                    "GenDI.SourceGenerator.csproj"
                )}" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
                  </ItemGroup>
                </Project>
                """
            );
            await File.WriteAllTextAsync(projectPath, fsproj, Token);

            await File.WriteAllTextAsync(
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
                """,
                Token
            );

            var failure = await RunDotnetCommandAsync(
                $"build \"{projectPath}\" -nologo",
                tempRoot,
                expectSuccess: false,
                Token
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
            var parent = Directory.GetParent(root) ?? throw new DirectoryNotFoundException(
                "Could not locate repository root containing GenDI.slnx."
            );
            root = parent.FullName;
        }

        return root;
    }

    private static async Task<string> RunDotnetCommandAsync(
        string arguments,
        string? workingDirectory = null,
        bool expectSuccess = true,
        CancellationToken cancellationToken = default
    )
    {
        var (ExitCode, Output) = await TryRunDotnetCommandAsync(arguments, workingDirectory, cancellationToken);

        if (expectSuccess)
        {
            Assert.True(
                ExitCode == 0,
                $"dotnet {arguments} failed with exit code {ExitCode}.{Environment.NewLine}{Output}"
            );
        }
        else
        {
            Assert.NotEqual(0, ExitCode);
        }

        return Output;
    }

    private static async Task<(int ExitCode, string Output)> TryRunDotnetCommandAsync(
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default
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
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (
            process.ExitCode,
            $"STDOUT:{Environment.NewLine}{output}{Environment.NewLine}STDERR:{Environment.NewLine}{error}"
        );
    }
}
