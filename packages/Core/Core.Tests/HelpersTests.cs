using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Core;
using Supabase.Core.Attributes;

namespace Core.Tests;

/// <summary>
/// Covers <see cref="Helpers"/>: reflective reads of a property value, a type-level custom attribute
/// (from an instance or a type), and the <see cref="MapToAttribute"/> mapped onto an enum member.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class HelpersTests
{
    [TestMethod]
    public void GetPropertyValue_ShouldReturnThePropertyValue() =>
        Helpers.GetPropertyValue<string>(new Decorated(), nameof(Decorated.Name)).Should().Be("value");

    [TestMethod]
    public void GetCustomAttribute_ShouldReturnTheAttribute_GivenAnInstance() =>
        Helpers.GetCustomAttribute<DescriptorAttribute>(new Decorated()).Should().NotBeNull();

    [TestMethod]
    public void GetCustomAttribute_ShouldReturnTheAttribute_GivenAType() =>
        Helpers.GetCustomAttribute<DescriptorAttribute>(typeof(Decorated)).Should().NotBeNull();

    [TestMethod]
    public void GetMappedToAttr_ShouldReturnTheMapping_GivenAMappedEnumMember() =>
        Helpers.GetMappedToAttr(Grant.RefreshToken).Mapping.Should().Be("refresh_token");

    [TestMethod]
    public void GetMappedToAttr_ShouldReturnNull_GivenAnUnmappedEnumMember() =>
        Helpers.GetMappedToAttr(Grant.Password).Should().BeNull();

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class DescriptorAttribute : Attribute
    {
    }

    [Descriptor]
    private sealed class Decorated
    {
        public string Name { get; } = "value";
    }

    private enum Grant
    {
        [MapTo("refresh_token")]
        RefreshToken,
        Password
    }
}
