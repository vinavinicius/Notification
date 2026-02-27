using System.Text.RegularExpressions;

namespace UEAT.Notification.Core.ValueObjects;

public sealed record MobilePhone
{
    private string CountryCode { get; }
    private string AreaCode { get; }
    private string Number { get; }

    public string FullNumber => $"+{CountryCode}{AreaCode}{Number}";

    public MobilePhone(string countryCode, string areaCode, string number)
    {
        ArgumentNullException.ThrowIfNull(countryCode);
        ArgumentNullException.ThrowIfNull(areaCode);
        ArgumentNullException.ThrowIfNull(number);
        
        if (!Regex.IsMatch(countryCode ?? "", @"^\d{1,3}$"))
            throw new ArgumentException("Invalid country code.", nameof(countryCode));

        if (!Regex.IsMatch(areaCode ?? "", @"^\d{2,3}$"))
            throw new ArgumentException("Invalid area code.", nameof(areaCode));

        if (!Regex.IsMatch(number ?? "", @"^\d{7,9}$"))
            throw new ArgumentException("Invalid number.", nameof(number));

        CountryCode = countryCode;
        AreaCode = areaCode;
        Number = number;
    }
}