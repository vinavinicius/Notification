using FluentAssertions;
using UEAT.Notification.Core.ValueObjects;

namespace UEAT.Notification.Tests.Core.ValueObjects;

public class MobilePhoneTests
{
    [Theory]
    [InlineData("1",   "11",  "1234567",   "+11112345 67")]   // country 1d, number 7d
    [InlineData("1",   "11",  "12345678",  "+111123456 78")]  // country 1d, number 8d
    [InlineData("1",   "11",  "123456789", "+1111234567 89")] // country 1d, number 9d
    [InlineData("55",  "11",  "1234567",   "+55111234567")]   // country 2d
    [InlineData("123", "11",  "1234567",   "+123111234567")]  // country 3d
    [InlineData("1",   "021", "1234567",   "+10211234567")]   // area 3d
    public void Constructor_ValidCombinations_CreatesInstance(
        string countryCode, string areaCode, string number, string _)
    {
        var act = () => new MobilePhone(countryCode, areaCode, number);

        act.Should().NotThrow();
    }
    
    [Theory]
    [InlineData("1",   "514", "5551234",   "+15145551234")]
    [InlineData("55",  "11",  "987654321", "+5511987654321")]
    [InlineData("123", "021", "1234567",   "+1230211234567")]
    public void FullNumber_ReturnsE164Format(
        string countryCode, string areaCode, string number, string expected)
    {
        var phone = new MobilePhone(countryCode, areaCode, number);

        phone.FullNumber.Should().Be(expected);
    }

    [Fact]
    public void FullNumber_AlwaysStartsWithPlus()
    {
        var phone = new MobilePhone("1", "514", "5551234");

        phone.FullNumber.Should().StartWith("+");
    }

    [Fact]
    public void ToString_ReturnsSameValueAsFullNumber()
    {
        var phone = new MobilePhone("55", "11", "987654321");

        phone.ToString().Should().Be(phone.FullNumber);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceCountryCode_ThrowsArgumentException(string? countryCode)
    {
        var act = () => new MobilePhone(countryCode!, "11", "1234567");

        act.Should().Throw<ArgumentException>()
           .WithParameterName("countryCode");
    }
    
    [Theory]
    [InlineData("1234",  "4 dígitos — excede o limite de 3")]
    [InlineData("AB",    "letras não são permitidas")]
    [InlineData("1 2",   "espaço interno não é permitido")]
    [InlineData("+55",   "sinal '+' não é permitido — o prefixo é adicionado internamente")]
    [InlineData("1.2",   "ponto não é permitido")]
    public void Constructor_InvalidCountryCode_ThrowsArgumentException(
        string countryCode, string reason)
    {
        var act = () => new MobilePhone(countryCode, "11", "1234567");

        act.Should().Throw<ArgumentException>(reason)
           .WithParameterName("countryCode");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceAreaCode_ThrowsArgumentException(string? areaCode)
    {
        var act = () => new MobilePhone("1", areaCode!, "1234567");

        act.Should().Throw<ArgumentException>()
           .WithParameterName("areaCode");
    }
    
    [Theory]
    [InlineData("1",    "1 dígito — abaixo do mínimo de 2")]
    [InlineData("1234", "4 dígitos — excede o limite de 3")]
    [InlineData("AB",   "letras não são permitidas")]
    [InlineData("1 1",  "espaço interno não é permitido")]
    public void Constructor_InvalidAreaCode_ThrowsArgumentException(
        string areaCode, string reason)
    {
        var act = () => new MobilePhone("1", areaCode, "1234567");

        act.Should().Throw<ArgumentException>(reason)
           .WithParameterName("areaCode");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceNumber_ThrowsArgumentException(string? number)
    {
        var act = () => new MobilePhone("1", "11", number!);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("number");
    }

    [Theory]
    [InlineData("123456",     "6 dígitos — abaixo do mínimo de 7")]
    [InlineData("1234567890", "10 dígitos — excede o limite de 9")]
    [InlineData("ABCDEFG",    "letras não são permitidas")]
    [InlineData("123 4567",   "espaço interno não é permitido")]
    [InlineData("123-4567",   "hífen não é permitido")]
    public void Constructor_InvalidNumber_ThrowsArgumentException(
        string number, string reason)
    {
        var act = () => new MobilePhone("1", "11", number);

        act.Should().Throw<ArgumentException>(reason)
           .WithParameterName("number");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new MobilePhone("1", "514", "5551234");
        var b = new MobilePhone("1", "514", "5551234");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Theory]
    [InlineData("1",  "514", "5551234",  "55", "514", "5551234",  "country code diferente")]
    [InlineData("1",  "514", "5551234",  "1",  "416", "5551234",  "area code diferente")]
    [InlineData("1",  "514", "5551234",  "1",  "514", "5559999",  "número diferente")]
    public void Equality_DifferentValues_AreNotEqual(
        string cc1, string ac1, string n1,
        string cc2, string ac2, string n2,
        string reason)
    {
        var a = new MobilePhone(cc1, ac1, n1);
        var b = new MobilePhone(cc2, ac2, n2);

        a.Should().NotBe(b, reason);
        (a != b).Should().BeTrue(reason);
    }

    [Fact]
    public void Equality_SameReference_IsEqual()
    {
        var phone = new MobilePhone("1", "514", "5551234");

        phone.Should().Be(phone);
    }

    [Fact]
    public void GetHashCode_EqualInstances_ReturnSameHash()
    {
        var a = new MobilePhone("1", "514", "5551234");
        var b = new MobilePhone("1", "514", "5551234");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
    
    [Fact]
    public void Constructor_CountryCode_ExactlyOnDigit_IsValid()
    {
        var act = () => new MobilePhone("1", "11", "1234567");
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_CountryCode_ExactlyThreeDigits_IsValid()
    {
        var act = () => new MobilePhone("123", "11", "1234567");
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_AreaCode_ExactlyTwoDigits_IsValid()
    {
        var act = () => new MobilePhone("1", "11", "1234567");
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_AreaCode_ExactlyThreeDigits_IsValid()
    {
        var act = () => new MobilePhone("1", "514", "1234567");
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_Number_ExactlySevenDigits_IsValid()
    {
        var act = () => new MobilePhone("1", "11", "1234567");
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_Number_ExactlyNineDigits_IsValid()
    {
        var act = () => new MobilePhone("1", "11", "123456789");
        act.Should().NotThrow();
    }
}