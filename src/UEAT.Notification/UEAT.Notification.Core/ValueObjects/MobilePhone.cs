using System.Text.RegularExpressions;

namespace UEAT.Notification.Core.ValueObjects;

public sealed record MobilePhone
{
    private static readonly Regex CountryCodeRegex = new(@"^\d{1,3}$", RegexOptions.Compiled);
    private static readonly Regex AreaCodeRegex = new(@"^\d{2,3}$", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"^\d{7,9}$", RegexOptions.Compiled);

    private string CountryCode { get; }
    private string AreaCode { get; }
    private string Number { get; }
    public string FullNumber => $"+{CountryCode}{AreaCode}{Number}";

    public MobilePhone(string countryCode, string areaCode, string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(areaCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        if (!CountryCodeRegex.IsMatch(countryCode))
            throw new ArgumentException("Country code must be 1–3 digits.", nameof(countryCode));

        if (!AreaCodeRegex.IsMatch(areaCode))
            throw new ArgumentException("Area code must be 2–3 digits.", nameof(areaCode));

        if (!NumberRegex.IsMatch(number))
            throw new ArgumentException("Number must be 7–9 digits.", nameof(number));

        CountryCode = countryCode;
        AreaCode = areaCode;
        Number = number;
    }
    
    public override string ToString() => FullNumber;
}