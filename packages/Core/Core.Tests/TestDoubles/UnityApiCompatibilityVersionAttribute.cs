using System;
using Core.Tests.TestDoubles;

// Util.GetUnityVersion scans loaded assemblies for a custom attribute whose type name is exactly
// "UnityAPICompatibilityVersionAttribute" (Unity stamps this on its player assemblies). Declaring the
// same shape here and applying it to the test assembly lets the unity branch resolve a real version
// hermetically, without a Unity install.
[assembly: UnityAPICompatibilityVersion("2022.3.5f1")]

namespace Core.Tests.TestDoubles;

[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class UnityAPICompatibilityVersionAttribute : Attribute
{
    public UnityAPICompatibilityVersionAttribute(string version) => this.Version = version;

    public string Version { get; }
}
