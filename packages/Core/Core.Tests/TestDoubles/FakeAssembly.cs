using System;
using System.Reflection;
using NSubstitute;

namespace Core.Tests.TestDoubles;

/// <summary>
/// Builds <see cref="Assembly"/> substitutes with a controlled name, version, and informational
/// version, so the assembly-probing helpers in <c>Supabase.Core</c> can be driven down every branch
/// without depending on which assemblies happen to be loaded in the test host.
/// </summary>
internal static class FakeAssembly
{
    internal static Assembly Named(string name, string? informationalVersion = null, Version? version = null)
    {
        var assembly = Substitute.For<Assembly>();
        assembly.GetName().Returns(new AssemblyName(name) { Version = version });
        var attributes = informationalVersion is null
            ? Array.Empty<Attribute>()
            : new Attribute[] { new AssemblyInformationalVersionAttribute(informationalVersion) };
        assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), Arg.Any<bool>())
            .Returns(attributes);
        return assembly;
    }
}
