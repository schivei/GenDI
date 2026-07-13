using System;
using Microsoft.CodeAnalysis;
using Xunit;

namespace GenDI.SourceGenerator.Tests;

public class HostedServiceGeneratorTests
{
    private const string HostedMissingContractDiagnosticId = "GENDISG002";

    // Self-contained stand-ins for Microsoft.Extensions.Hosting so the generator tests
    // do not depend on the hosting assembly being present (and unambiguous) on the
    // reference set. Everything is fully qualified because the shared generator harness
    // injects an assembly-level attribute ahead of the user source, which would
    // invalidate any leading using directives. The real BackgroundService/IHostedService
    // are exercised end-to-end by tests/GenDI.Phase6.WorkerValidation.App.
    private const string HostingStub = """
        namespace Microsoft.Extensions.Hosting
        {
            public interface IHostedService
            {
                System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken);
                System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken);
            }

            public abstract class BackgroundService : IHostedService
            {
                public virtual System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken) =>
                    System.Threading.Tasks.Task.CompletedTask;

                public virtual System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) =>
                    System.Threading.Tasks.Task.CompletedTask;

                protected abstract System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken);
            }
        }

        """;

    [Fact]
    public void Hosted_background_service_registers_via_AddHostedService_with_property_injection()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            HostingStub
                + """
                namespace HostedNs
                {
                    public interface IPinger
                    {
                        void Ping();
                    }

                    [Hosted]
                    public sealed class Worker : global::Microsoft.Extensions.Hosting.BackgroundService
                    {
                        [Inject]
                        public required IPinger Pinger { get; init; }

                        protected override System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken) =>
                            System.Threading.Tasks.Task.CompletedTask;
                    }
                }
                """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddHostedService<global::HostedNs.Worker>(static serviceProvider =>",
            generatedSource,
            StringComparison.Ordinal
        );
        // The generator harness compiles with nullable disabled, so the oblivious IPinger
        // reference resolves through the optional GetService path.
        Assert.Contains(
            "@Pinger = serviceProvider.GetService<global::HostedNs.IPinger>()",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Hosted_service_registers_constructor_injected_dependencies()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            HostingStub
                + """
                namespace HostedNs
                {
                    public interface IPinger
                    {
                        void Ping();
                    }

                    [Hosted]
                    public sealed class CtorWorker(IPinger pinger) : global::Microsoft.Extensions.Hosting.BackgroundService
                    {
                        protected override System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
                        {
                            pinger.Ping();
                            return System.Threading.Tasks.Task.CompletedTask;
                        }
                    }
                }
                """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        // Nullable is disabled in the generator harness, so the constructor parameter
        // resolves through the optional GetService path.
        Assert.Contains(
            "services.AddHostedService<global::HostedNs.CtorWorker>(static serviceProvider => new global::HostedNs.CtorWorker(serviceProvider.GetService<global::HostedNs.IPinger>()))",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Hosted_service_directly_implementing_IHostedService_is_registered()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            HostingStub
                + """
                namespace HostedNs
                {
                    [Hosted]
                    public sealed class DirectWorker : global::Microsoft.Extensions.Hosting.IHostedService
                    {
                        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken) =>
                            System.Threading.Tasks.Task.CompletedTask;

                        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) =>
                            System.Threading.Tasks.Task.CompletedTask;
                    }
                }
                """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddHostedService<global::HostedNs.DirectWorker>(static serviceProvider => new global::HostedNs.DirectWorker())",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Hosted_service_detected_through_intermediate_base_chain()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            HostingStub
                + """
                namespace HostedNs
                {
                    public abstract class WorkerBase : global::Microsoft.Extensions.Hosting.BackgroundService
                    {
                    }

                    [Hosted]
                    public sealed class DerivedWorker : WorkerBase
                    {
                        protected override System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken) =>
                            System.Threading.Tasks.Task.CompletedTask;
                    }
                }
                """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddHostedService<global::HostedNs.DerivedWorker>(static serviceProvider => new global::HostedNs.DerivedWorker())",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Hosted_class_without_IHostedService_reports_diagnostic_and_is_not_registered()
    {
        var userSource =
            HostingStub
            + """
                namespace HostedNs
                {
                    [Hosted]
                    public sealed class NotAHostedService
                    {
                    }
                }
                """;

        var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
            userSource,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        var diagnostic = Assert.Single(
            diagnostics,
            d => string.Equals(d.Id, HostedMissingContractDiagnosticId, StringComparison.Ordinal)
        );
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains(
            "global::HostedNs.NotAHostedService",
            diagnostic.GetMessage(),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Hosted_service_without_dependencies_does_not_report_diagnostic()
    {
        var userSource =
            HostingStub
            + """
                namespace HostedNs
                {
                    [Hosted]
                    public sealed class Worker : global::Microsoft.Extensions.Hosting.BackgroundService
                    {
                        protected override System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken) =>
                            System.Threading.Tasks.Task.CompletedTask;
                    }
                }
                """;

        var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
            userSource,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.DoesNotContain(
            diagnostics,
            d => string.Equals(d.Id, HostedMissingContractDiagnosticId, StringComparison.Ordinal)
        );
    }
}
