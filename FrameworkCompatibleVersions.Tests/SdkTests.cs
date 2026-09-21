using System.Xml.Linq;
using Xunit;

namespace FrameworkCompatibleVersions.Tests;

public sealed class SdkTests
{
    private static readonly string SdkDirectory = Path.Combine(AppContext.BaseDirectory, "Sdk");

    [Fact]
    public void SdkFilesAreValidXml()
    {
        _ = XDocument.Load(Path.Combine(SdkDirectory, "Sdk.props"));
        _ = XDocument.Load(Path.Combine(SdkDirectory, "Sdk.targets"));
    }

    [Fact]
    public void SdkIsPackageAgnostic()
    {
        XDocument targets = XDocument.Load(Path.Combine(SdkDirectory, "Sdk.targets"));

        Assert.DoesNotContain(targets.Descendants("PackageVersion"), item => item.Attribute("Include") is not null);
        Assert.DoesNotContain(targets.Descendants("PackageReference"), item => item.Attribute("Include") is not null);
    }

    [Fact]
    public void OptInMetadataUsesExpectedName()
    {
        string text = File.ReadAllText(Path.Combine(SdkDirectory, "Sdk.targets"));

        Assert.Contains("WithMetadataValue('FrameworkCompatibleVersion', 'true')", text, StringComparison.Ordinal);
        Assert.DoesNotContain("WithMetadataValue('FrameworkCompatible', 'true')", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("HighestTestedAgainst")]
    [InlineData("MatchingMajor")]
    public void OptInMetadataSupportsAdditionalValues(string metadataValue)
    {
        string text = File.ReadAllText(Path.Combine(SdkDirectory, "Sdk.targets"));

        Assert.Contains($"WithMetadataValue('FrameworkCompatibleVersion', '{metadataValue}')", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CentralPackageFloatingVersionsAreEnabledForCpm()
    {
        XDocument targets = XDocument.Load(Path.Combine(SdkDirectory, "Sdk.targets"));
        XElement property = targets.Descendants("CentralPackageFloatingVersionsEnabled").Single();

        Assert.Equal("true", property.Value);
    }

    [Theory]
    [InlineData("[6.*,7.0.0)")]
    [InlineData("[8.*,9.0.0)")]
    [InlineData("[10.*,11.0.0)")]
    [InlineData("[*,13.0.0)")]
    public void ContainsExpectedVersionPolicy(string expected)
    {
        string targets = File.ReadAllText(Path.Combine(SdkDirectory, "Sdk.targets"));

        Assert.Contains(expected, targets, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[5.*,6.0.0)")]
    [InlineData("[7.*,8.0.0)")]
    [InlineData("[9.*,10.0.0)")]
    [InlineData("[3.*,4.0.0)")]
    [InlineData("[2.*,3.0.0)")]
    public void ContainsExpectedMatchingMajorPolicy(string expected)
    {
        string targets = File.ReadAllText(Path.Combine(SdkDirectory, "Sdk.targets"));

        Assert.Contains(expected, targets, StringComparison.Ordinal);
    }
}
