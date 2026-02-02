using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace GotrueTests
{
	public static class TestUtils
	{
		private static readonly Random Random = new Random();

		public const string PASSWORD = "I@M@SuperP@ssWord";
		
		public static void LogDebug(string message, Exception e)
		{
			Debug.WriteLine(message);
			if (e != null)
				Debug.WriteLine(e);
		}


		public static string RandomString(int length)
		{
			const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
			return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
		}

		public static string GetRandomPhoneNumber()
		{
			const string chars = "123456789";
			var inner = new string(Enumerable.Repeat(chars, 10).Select(s => s[Random.Next(s.Length)]).ToArray());
			return $"+1{inner}";
		}

		/// <summary>
		/// Returns a random number within the limits specified via parameters.
		/// </summary>
		/// <param name="minValue">Minimum value. Default 0.</param>
		/// <param name="maxValue">Maximum value. Default 1000.</param>
		/// <returns>Integer within the range.</returns>
		public static int RandomNumber(int minValue = 0, int maxValue = 1000)
		{
			return Random.Next(minValue, maxValue);
		}

		public static string GenerateServiceRoleToken()
		{
			var jwksJson = GetJwkJson();
			var key = CreateEcdsaSecurityKeyFromJwksJson(jwksJson);

			var creds = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);

			var header = new JwtHeader(creds);
			header["kid"] = key.KeyId;

			var claims = new List<Claim>
			{
				new("role", "service_role"),
			};

			var token = new JwtSecurityToken(
				header: header,
				payload: new JwtPayload(
					issuer: null,
					audience: null,
					claims: claims,
					notBefore: DateTime.UtcNow.AddMinutes(-1),
					expires: DateTime.UtcNow.AddDays(7),
					issuedAt: DateTime.UtcNow
				)
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}


		private static string GetRepoRoot()
		{
			// Start from where the process is running and walk upwards until we find the solution file
			for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
			{
				if (File.Exists(Path.Combine(dir.FullName, "Supabase.sln")))
					return dir.FullName;
			}

			// Fallback: start from the assembly location
			for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
			{
				if (File.Exists(Path.Combine(dir.FullName, "Supabase.sln")))
					return dir.FullName;
			}

			throw new InvalidOperationException("Repo root not found (Supabase.sln missing in parent chain).");
		}
		
		private static string GetJwkJson()
		{
			var path = Path.Combine(GetRepoRoot(), "supabase", "signing_keys.json");
			return File.ReadAllText(path);
		}
		
		private static ECDsaSecurityKey CreateEcdsaSecurityKeyFromJwksJson(string jwksJson)
		{
			using var doc = JsonDocument.Parse(jwksJson);

			if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() < 1)
				throw new InvalidOperationException("JWKS JSON must be an array with at least one JWK object.");

			var jwk = doc.RootElement[0];

			var kty = jwk.GetProperty("kty").GetString();
			var crv = jwk.GetProperty("crv").GetString();
			var alg = jwk.TryGetProperty("alg", out var algProp) ? algProp.GetString() : null;

			if (!string.Equals(kty, "EC", StringComparison.OrdinalIgnoreCase))
				throw new NotSupportedException($"Unsupported kty '{kty}'. Expected 'EC'.");

			if (!string.Equals(crv, "P-256", StringComparison.OrdinalIgnoreCase))
				throw new NotSupportedException($"Unsupported crv '{crv}'. Expected 'P-256' for ES256.");

			if (alg is not null && !string.Equals(alg, "ES256", StringComparison.OrdinalIgnoreCase))
				throw new NotSupportedException($"JWK alg is '{alg}', but this code expects 'ES256'.");

			var x = Base64UrlDecode(jwk.GetProperty("x").GetString() ?? throw new InvalidOperationException("Missing JWK 'x'."));
			var y = Base64UrlDecode(jwk.GetProperty("y").GetString() ?? throw new InvalidOperationException("Missing JWK 'y'."));
			var d = Base64UrlDecode(jwk.GetProperty("d").GetString() ?? throw new InvalidOperationException("Missing JWK 'd' (private key)."));

			var kid = jwk.TryGetProperty("kid", out var kidProp) ? kidProp.GetString() : null;
			if (string.IsNullOrWhiteSpace(kid))
				throw new InvalidOperationException("Missing JWK 'kid'.");

			var ecParams = new ECParameters
			{
				Curve = ECCurve.NamedCurves.nistP256,
				Q = new ECPoint { X = x, Y = y },
				D = d
			};

			var ecdsa = ECDsa.Create(ecParams);
			return new ECDsaSecurityKey(ecdsa) { KeyId = kid };
		}

		private static byte[] Base64UrlDecode(string input)
		{
			string s = input.Replace('-', '+').Replace('_', '/');
			switch (s.Length % 4)
			{
				case 2: s += "=="; break;
				case 3: s += "="; break;
				case 0: break;
				default: throw new FormatException("Invalid base64url string length.");
			}
			return Convert.FromBase64String(s);
		}
		
	}
}
