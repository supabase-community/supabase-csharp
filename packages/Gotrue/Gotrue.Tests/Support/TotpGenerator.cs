// This code is adapted from a GitHub Gist by @jimbojetset [https://gist.github.com/jimbojetset/c7944fd3e900b70a61cf]

#region

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

#endregion

namespace Gotrue.Tests.Support;

/// <summary>
///     Generates the time-based one-time password an authenticator app would produce for an enrolled MFA
///     factor, so the MFA E2E tests can complete a real challenge/verify against the live stack.
/// </summary>
internal static class TotpGenerator
{
    public static string GeneratePin(string base32secret, int interval, int pinLength)
    {
        if (IsBase32(base32secret) && (interval == 30 || interval == 60) && (pinLength == 6 || pinLength == 8))
        {
            var secretBytes = Base32StringToBytes(base32secret);
            var unixTimeBytes = BitConverter.GetBytes((long) Math.Floor((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) / interval);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(unixTimeBytes);
            }
            var hashBytes = new HMACSHA1(secretBytes).ComputeHash(unixTimeBytes);
            var offset = hashBytes[hashBytes.Length - 1] & 0xF;
            var bytes = new byte[4];
            Buffer.BlockCopy(hashBytes, offset, bytes, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            var integer = BitConverter.ToInt32(bytes, 0);
            var truncated = integer & 0x7FFFFFFF;
            var pin = truncated % 1000000;
            return pin.ToString(CultureInfo.InvariantCulture).PadLeft(pinLength, '0');
        }
        return string.Empty;
    }

    private static byte[] Base32StringToBytes(string input)
    {
        input = input.TrimEnd('=');
        var byteCount = input.Length * 5 / 8;
        var returnBytes = new byte[byteCount];
        byte curByte = 0;
        byte bitsRemaining = 8;
        int mask;
        var arrayIndex = 0;
        foreach (var c in input)
        {
            var cValue = CharToValue(c);
            if (bitsRemaining > 5)
            {
                mask = cValue << bitsRemaining - 5;
                curByte = (byte) (curByte | mask);
                bitsRemaining -= 5;
            }
            else
            {
                mask = cValue >> 5 - bitsRemaining;
                curByte = (byte) (curByte | mask);
                returnBytes[arrayIndex++] = curByte;
                curByte = (byte) (cValue << 3 + bitsRemaining);
                bitsRemaining += 3;
            }
        }
        if (arrayIndex != byteCount)
        {
            returnBytes[arrayIndex] = curByte;
        }
        return returnBytes;
    }

    private static int CharToValue(char c)
    {
        int value = c;
        if (value < 91 && value > 64)
        {
            return value - 65;
        }
        if (value < 56 && value > 49)
        {
            return value - 24;
        }
        if (value < 123 && value > 96)
        {
            return value - 97;
        }
        throw new ArgumentException($"Character '{c}' is not a valid Base32 character.", nameof(c));
    }

    private static bool IsBase32(string b32)
    {
        var regex = new Regex(@"^[A-Z2-7]+=*$");
        var match = regex.Match(b32);
        return b32.Length % 8 == 0 && match.Success;
    }
}
