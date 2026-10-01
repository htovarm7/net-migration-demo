namespace AuthManager.Api.Auth;

public enum PasswordHashFormat
{
    Unknown,
    /// <summary>Identity v1/v2 (.NET Framework): marker 0x00, PBKDF2-HMAC-SHA1, 1000 iterations.</summary>
    IdentityV2,
    /// <summary>Identity v3 (.NET Core+): marker 0x01, PBKDF2 with PRF/iterations stored in the header.</summary>
    IdentityV3
}

public static class PasswordHashFormatDetector
{
    public static PasswordHashFormat Detect(string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return PasswordHashFormat.Unknown;

        try
        {
            var bytes = Convert.FromBase64String(hash);
            if (bytes.Length == 0) return PasswordHashFormat.Unknown;
            return bytes[0] switch
            {
                0x00 => PasswordHashFormat.IdentityV2,
                0x01 => PasswordHashFormat.IdentityV3,
                _ => PasswordHashFormat.Unknown
            };
        }
        catch (FormatException)
        {
            return PasswordHashFormat.Unknown;
        }
    }
}
