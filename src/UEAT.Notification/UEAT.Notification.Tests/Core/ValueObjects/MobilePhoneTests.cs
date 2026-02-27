using UEAT.Notification.Core.ValueObjects;

namespace UEAT.Notification.Tests.Core.ValueObjects;

public class MobilePhoneTests
{
    [Fact]
    public void Constructor_ValidArguments_CreatesInstance()
    {
        var phone = new MobilePhone("55", "11", "987654321");

        Assert.NotNull(phone);
    }

    [Theory]
    [InlineData("1", "11", "1234567")] // 1-digit country code, 7-digit number
    [InlineData("55", "11", "987654321")] // 2-digit country code, 9-digit number
    [InlineData("123", "11", "12345678")] // 3-digit country code, 8-digit number
    [InlineData("55", "021", "1234567")] // 3-digit area code
    public void Constructor_BoundaryValidValues_CreatesInstance(
        string countryCode, string areaCode, string number)
    {
        var phone = new MobilePhone(countryCode, areaCode, number);
        Assert.NotNull(phone);
    }

    [Fact]
    public void FullNumber_ReturnsFormattedNumber()
    {
        var phone = new MobilePhone("55", "11", "987654321");

        Assert.Equal("+5511987654321", phone.FullNumber);
    }

    [Fact]
    public void ToString_ReturnsFullNumber()
    {
        var phone = new MobilePhone("55", "11", "987654321");

        Assert.Equal(phone.FullNumber, phone.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceCountryCode_ThrowsArgumentException(string? countryCode)
    {
        Assert.Throws<ArgumentException>(() => new MobilePhone(countryCode!, "11", "987654321"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceAreaCode_ThrowsArgumentException(string? areaCode)
    {
        Assert.Throws<ArgumentException>(() => new MobilePhone("55", areaCode!, "987654321"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceNumber_ThrowsArgumentException(string? number)
    {
        Assert.Throws<ArgumentException>(() => new MobilePhone("55", "11", number!));
    }
    

    [Theory]
    [InlineData("1234")] // 4 digits – too long
    [InlineData("AB")] // letters
    [InlineData("1 2")] // space inside
    [InlineData("+55")] // leading plus sign
    public void Constructor_InvalidCountryCode_ThrowsArgumentException(string countryCode)
    {
        var ex = Assert.Throws<ArgumentException>(() => new MobilePhone(countryCode, "11", "987654321"));

        Assert.Equal("countryCode", ex.ParamName);
    }

    [Theory]
    [InlineData("1")] // 1 digit – too short
    [InlineData("1234")] // 4 digits – too long
    [InlineData("AB")] // letters
    public void Constructor_InvalidAreaCode_ThrowsArgumentException(string areaCode)
    {
        var ex = Assert.Throws<ArgumentException>(() => new MobilePhone("55", areaCode, "987654321"));

        Assert.Equal("areaCode", ex.ParamName);
    }

    [Theory]
    [InlineData("123456")] // 6 digits – too short
    [InlineData("1234567890")] // 10 digits – too long
    [InlineData("ABCDEFG")] // letters
    [InlineData("123 4567")] // space inside
    public void Constructor_InvalidNumber_ThrowsArgumentException(string number)
    {
        var ex = Assert.Throws<ArgumentException>(() => new MobilePhone("55", "11", number));

        Assert.Equal("number", ex.ParamName);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var phone1 = new MobilePhone("55", "11", "987654321");
        var phone2 = new MobilePhone("55", "11", "987654321");

        Assert.Equal(phone1, phone2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var phone1 = new MobilePhone("55", "11", "987654321");
        var phone2 = new MobilePhone("55", "21", "987654321");

        Assert.NotEqual(phone1, phone2);
    }
}