using Visa2026.Module.Appearance;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Appearance;

public sealed class OptionalDetailFieldsMetadataTests
{
    [Fact]
    public void Supports_TrueForAttributedTypes()
    {
        Assert.True(OptionalDetailFieldsMetadata.Supports(typeof(Person)));
        Assert.True(OptionalDetailFieldsMetadata.Supports(typeof(Passport)));
        Assert.True(OptionalDetailFieldsMetadata.Supports(typeof(Education)));
    }

    [Fact]
    public void Supports_FalseForUnaffectedTypes()
    {
        Assert.False(OptionalDetailFieldsMetadata.Supports(typeof(string)));
        Assert.False(OptionalDetailFieldsMetadata.Supports(typeof(ApplicationProgress)));
        Assert.False(OptionalDetailFieldsMetadata.Supports(null));
    }

    [Theory]
    [InlineData(null, typeof(string), false)]
    [InlineData("", typeof(string), false)]
    [InlineData("   ", typeof(string), false)]
    [InlineData("note", typeof(string), true)]
    public void HasMeaningfulOptionalValue_Strings(object value, System.Type memberType, bool expected)
    {
        Assert.Equal(expected, OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(value, memberType));
    }

    [Fact]
    public void HasMeaningfulOptionalValue_DateTimeDefaultIsEmpty()
    {
        Assert.False(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(default(System.DateTime), typeof(System.DateTime)));
        Assert.True(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(
            new System.DateTime(2024, 1, 2, 0, 0, 0, System.DateTimeKind.Unspecified),
            typeof(System.DateTime)));
    }

    [Fact]
    public void HasMeaningfulOptionalValue_BoolOnlyTrueCounts()
    {
        Assert.False(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(false, typeof(bool)));
        Assert.True(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(true, typeof(bool)));
    }

    [Fact]
    public void HasMeaningfulOptionalValue_ByteArrayRequiresContent()
    {
        Assert.False(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(System.Array.Empty<byte>(), typeof(byte[])));
        Assert.True(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(new byte[] { 1 }, typeof(byte[])));
    }

    private enum SampleEnum
    {
        None = 0,
        Other = 1,
    }

    [Fact]
    public void HasMeaningfulOptionalValue_EnumsAlwaysMeaningfulWhenNonNull()
    {
        Assert.True(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(SampleEnum.None, typeof(SampleEnum)));
        Assert.True(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(SampleEnum.Other, typeof(SampleEnum?)));
    }

    [Fact]
    public void HasMeaningfulOptionalValue_NonStringReferenceTypeIsMeaningful()
    {
        Assert.True(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(new object(), typeof(object)));
    }

    [Fact]
    public void HasMeaningfulOptionalValue_OtherValueTypesAreNotMeaningful()
    {
        Assert.False(OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(42, typeof(int)));
    }
}
