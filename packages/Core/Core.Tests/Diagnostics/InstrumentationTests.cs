using System;
using Core.Tests.TestDoubles;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Core.Diagnostics;

namespace Core.Tests.Diagnostics;

/// <summary>
/// Covers <see cref="Instrumentation"/>: the version resolved from an assembly (informational version
/// preferred, build metadata stripped, assembly version as fallback) and the naming/versioning of the
/// <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Metrics.Meter"/>
/// it produces.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class InstrumentationTests
{
    [TestMethod]
    public void GetVersion_ShouldReturnInformationalVersion() =>
        Instrumentation.GetVersion(FakeAssembly.Named("Lib", informationalVersion: "1.2.3"))
            .Should().Be("1.2.3");

    [TestMethod]
    public void GetVersion_ShouldStripBuildMetadata_GivenInformationalVersionWithMetadata() =>
        Instrumentation.GetVersion(FakeAssembly.Named("Lib", informationalVersion: "1.2.3+abc1234"))
            .Should().Be("1.2.3");

    [TestMethod]
    public void GetVersion_ShouldReturnVerbatim_GivenMetadataDelimiterAtStart() =>
        Instrumentation.GetVersion(FakeAssembly.Named("Lib", informationalVersion: "+onlymetadata"))
            .Should().Be("+onlymetadata", "the '+' at index 0 is not a metadata separator to strip");

    [TestMethod]
    public void GetVersion_ShouldFallBackToAssemblyVersion_GivenNoInformationalVersion() =>
        Instrumentation.GetVersion(FakeAssembly.Named("Lib", version: new Version(2, 5, 9)))
            .Should().Be("2.5.9");

    [TestMethod]
    public void GetVersion_ShouldFallBackToAssemblyVersion_GivenEmptyInformationalVersion() =>
        Instrumentation.GetVersion(FakeAssembly.Named("Lib", informationalVersion: "", version: new Version(2, 5, 9)))
            .Should().Be("2.5.9");

    [TestMethod]
    public void GetVersion_ShouldReturnZeroDefault_GivenNoVersionInformationAtAll() =>
        Instrumentation.GetVersion(FakeAssembly.Named("Lib"))
            .Should().Be("0.0.0");

    [TestMethod]
    public void CreateActivitySource_ShouldUseTheGivenNameAndAssemblyVersion()
    {
        var assembly = FakeAssembly.Named("Lib", informationalVersion: "3.1.4");
        using var source = Instrumentation.CreateActivitySource(assembly, "Supabase.Test");
        using (new AssertionScope())
        {
            source.Name.Should().Be("Supabase.Test");
            source.Version.Should().Be("3.1.4");
        }
    }

    [TestMethod]
    public void CreateMeter_ShouldUseTheGivenNameAndAssemblyVersion()
    {
        var assembly = FakeAssembly.Named("Lib", informationalVersion: "3.1.4");
        using var meter = Instrumentation.CreateMeter(assembly, "Supabase.Test");
        using (new AssertionScope())
        {
            meter.Name.Should().Be("Supabase.Test");
            meter.Version.Should().Be("3.1.4");
        }
    }
}
