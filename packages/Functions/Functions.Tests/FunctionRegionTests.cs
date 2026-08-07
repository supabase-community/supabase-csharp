using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Supabase.Functions.Client;

namespace Functions.Tests
{
    /// <summary>
    /// Covers <see cref="FunctionRegion"/>: the wire string carried by every named region, its value
    /// semantics (equality, hash code, operators) and the explicit conversions to and from
    /// <see cref="string"/>. The wire string is what ends up in the <c>x-region</c> header, so each
    /// constant is pinned to its exact value.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class FunctionRegionTests
    {
        [TestMethod]
        public void Region_ShouldExposeItsWireString()
        {
            using (new AssertionScope())
            {
                FunctionRegion.Any.ToString().Should().Be("any");
                FunctionRegion.ApNortheast1.ToString().Should().Be("ap-northeast-1");
                FunctionRegion.ApNortheast2.ToString().Should().Be("ap-northeast-2");
                FunctionRegion.ApSouth1.ToString().Should().Be("ap-south-1");
                FunctionRegion.ApSoutheast1.ToString().Should().Be("ap-southeast-1");
                FunctionRegion.ApSoutheast2.ToString().Should().Be("ap-southeast-2");
                FunctionRegion.CaCentral1.ToString().Should().Be("ca-central-1");
                FunctionRegion.EuCentral1.ToString().Should().Be("eu-central-1");
                FunctionRegion.EuWest1.ToString().Should().Be("eu-west-1");
                FunctionRegion.EuWest2.ToString().Should().Be("eu-west-2");
                FunctionRegion.EuWest3.ToString().Should().Be("eu-west-3");
                FunctionRegion.SaEast1.ToString().Should().Be("sa-east-1");
                FunctionRegion.UsEast1.ToString().Should().Be("us-east-1");
                FunctionRegion.UsWest1.ToString().Should().Be("us-west-1");
                FunctionRegion.UsWest2.ToString().Should().Be("us-west-2");
            }
        }

        [TestMethod]
        public void Region_ShouldConvertToItsWireString_GivenExplicitStringCast() =>
            ((string) FunctionRegion.UsEast1).Should().Be("us-east-1");

        [TestMethod]
        public void Region_ShouldCarryTheGivenString_GivenExplicitCastFromString() =>
            ((FunctionRegion) "custom-region").ToString().Should().Be("custom-region");

        [TestMethod]
        public void Region_ShouldEqualAnotherRegion_GivenSameWireString()
        {
            var left = (FunctionRegion) "us-east-1";
            var right = (FunctionRegion) "us-east-1";
            using (new AssertionScope())
            {
                left.Equals(right).Should().BeTrue();
                left.Equals((object) right).Should().BeTrue();
                (left == right).Should().BeTrue();
                (left != right).Should().BeFalse();
            }
        }

        [TestMethod]
        public void Region_ShouldNotEqualAnotherRegion_GivenDifferentWireString()
        {
            var left = FunctionRegion.UsEast1;
            var right = FunctionRegion.EuWest1;
            using (new AssertionScope())
            {
                left.Equals(right).Should().BeFalse();
                (left == right).Should().BeFalse();
                (left != right).Should().BeTrue();
            }
        }

        [TestMethod]
        public void Region_ShouldShareHashCodeWithItsWireString() =>
            FunctionRegion.UsEast1.GetHashCode().Should().Be("us-east-1".GetHashCode());
    }
}
