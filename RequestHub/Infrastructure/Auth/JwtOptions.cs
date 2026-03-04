namespace RequestHub.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "RequestHub";
    public string Audience { get; set; } = "RequestHubClient";
    public string Key { get; set; } = "super_secret_change_me_key_32_chars_min";
    public int ExpirationMinutes { get; set; } = 120;
}
