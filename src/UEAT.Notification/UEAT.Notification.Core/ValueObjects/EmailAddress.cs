namespace UEAT.Notification.Core.ValueObjects;

using System;
using System.Text.RegularExpressions;

public sealed record EmailAddress
{
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public string Address { get; }

    public EmailAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Email is required.", nameof(address));

        address = address.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(address))
            throw new ArgumentException("Invalid email format.", nameof(address));

        Address = address;
    }

    public override string ToString() => Address;
}