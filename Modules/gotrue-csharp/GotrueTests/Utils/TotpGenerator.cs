// This code is adapter from a GitHub Gist by @jimbojetset [https://gist.github.com/jimbojetset/c7944fd3e900b70a61cf]

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

class TotpGenerator
{
	public static string GeneratePin(string base32secret, int interval, int pinLength)
	{
		if (IsBase32(base32secret) && (interval == 30 || interval == 60) && (pinLength == 6 || pinLength == 8))
		{
			byte[] secretBytes = Base32StringToBytes(base32secret);
			byte[] unixTimeBytes = BitConverter.GetBytes(((long)Math.Floor((DateTime.UtcNow - (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalSeconds)) / interval);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(unixTimeBytes);
			byte[] hashBytes = new HMACSHA1(secretBytes).ComputeHash(unixTimeBytes);
			int Offset = hashBytes[hashBytes.Length - 1] & 0xF;
			byte[] bytes = new byte[4];
			Buffer.BlockCopy(hashBytes, Offset, bytes, 0, 4);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			int integer = BitConverter.ToInt32(bytes, 0);
			int truncated = integer & 0x7FFFFFFF;
			int pin = truncated % 1000000;
			return pin.ToString(CultureInfo.InvariantCulture).PadLeft(pinLength, '0');
		}
		return String.Empty;
	}

	private static byte[] Base32StringToBytes(string input)
	{
		input = input.TrimEnd('=');
		int byteCount = input.Length * 5 / 8;
		byte[] returnBytes = new byte[byteCount];
		byte curByte = 0;
		byte bitsRemaining = 8;
		int mask = 0;
		int arrayIndex = 0;
		foreach (char c in input)
		{
			int cValue = CharToValue(c);
			if (bitsRemaining > 5)
			{
				mask = cValue << (bitsRemaining - 5);
				curByte = (byte)(curByte | mask);
				bitsRemaining -= 5;
			}
			else
			{
				mask = cValue >> (5 - bitsRemaining);
				curByte = (byte)(curByte | mask);
				returnBytes[arrayIndex++] = curByte;
				curByte = (byte)(cValue << (3 + bitsRemaining));
				bitsRemaining += 3;
			}
		}
		if (arrayIndex != byteCount)
			returnBytes[arrayIndex] = curByte;
		return returnBytes;
	}

	private static int CharToValue(char c)
	{
		int value = (int)c;
		if (value < 91 && value > 64)
			return value - 65;
		if (value < 56 && value > 49)
			return value - 24;
		if (value < 123 && value > 96)
			return value - 97;

		throw new Exception();
	}

	public static bool IsBase32(string b32)
	{
		Regex regex = new Regex(@"^[A-Z2-7]+=*$");
		Match match = regex.Match(b32);
		bool b = b32.Length % 8 == 0 && match.Success;
		return b32.Length % 8 == 0 && match.Success;
	}
}