using System;
using FluentAssertions;
using UEAT.Notification.Core.ValueObjects;
using Xunit;

namespace UEAT.Notification.Library.Tests.Core.ValueObjects;

public class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("user+tag@domain.co.uk")]
    public void Constructor_ValidEmail_ShouldNormalizeToLowercase(string email)
    {
        var result = new EmailAddress(email);

        result.Address.Should().Be(email.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Constructor_NullOrEmpty_ShouldThrowArgumentException(string? email)
    {
        var act = () => new EmailAddress(email!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email is required*");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void Constructor_InvalidFormat_ShouldThrowArgumentException(string email)
    {
        var act = () => new EmailAddress(email);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email format*");
    }

    [Fact]
    public void ToString_ShouldReturnAddress()
    {
        var email = new EmailAddress("User@Example.com");

        email.ToString().Should().Be("user@example.com");
    }

    [Fact]
    public void Equality_SameAddress_ShouldBeEqual()
    {
        var a = new EmailAddress("user@example.com");
        var b = new EmailAddress("USER@EXAMPLE.COM");

        a.Should().Be(b);
    }
}
