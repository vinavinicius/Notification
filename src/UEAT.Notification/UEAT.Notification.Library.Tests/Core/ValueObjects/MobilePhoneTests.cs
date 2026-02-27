using System;
using FluentAssertions;
using UEAT.Notification.Core.ValueObjects;
using Xunit;

namespace UEAT.Notification.Library.Tests.Core.ValueObjects;

public class MobilePhoneTests
{
    [Fact]
    public void Constructor_ValidParts_ShouldBuildFullNumber()
    {
        var phone = new MobilePhone("1", "581", "5551234");

        phone.FullNumber.Should().Be("+15815551234");
    }

    [Theory]
    [InlineData("0000", "581", "5551234")] // country code > 3 digits
    [InlineData("abc", "581", "5551234")]  // non-numeric country code
    [InlineData("", "581", "5551234")]     // empty country code
    public void Constructor_InvalidCountryCode_ShouldThrowArgumentException(
        string countryCode, string areaCode, string number)
    {
        var act = () => new MobilePhone(countryCode, areaCode, number);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*country code*");
    }

    [Theory]
    [InlineData("1", "1", "5551234")]     // area code too short
    [InlineData("1", "12345", "5551234")] // area code too long
    [InlineData("1", "abc", "5551234")]   // non-numeric area code
    public void Constructor_InvalidAreaCode_ShouldThrowArgumentException(
        string countryCode, string areaCode, string number)
    {
        var act = () => new MobilePhone(countryCode, areaCode, number);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*area code*");
    }

    [Theory]
    [InlineData("1", "581", "123")]          // too short
    [InlineData("1", "581", "1234567890")]   // too long
    [InlineData("1", "581", "abcdefg")]      // non-numeric
    public void Constructor_InvalidNumber_ShouldThrowArgumentException(
        string countryCode, string areaCode, string number)
    {
        var act = () => new MobilePhone(countryCode, areaCode, number);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid number*");
    }

    [Fact]
    public void Constructor_NullCountryCode_ShouldThrowArgumentNullException()
    {
        var act = () => new MobilePhone(null!, "581", "5551234");

        act.Should().Throw<ArgumentNullException>();
    }
}
