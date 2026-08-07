using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Supabase.Postgrest.Converters
{

	/// <inheritdoc />
	public class DateTimeConverter : JsonConverter
	{
		/// <inheritdoc />
		public override bool CanConvert(Type objectType) => throw new NotImplementedException();

		/// <inheritdoc />
		public override bool CanWrite => true;

		/// <inheritdoc />
		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => 
			reader.TokenType == JsonToken.StartArray ? ReadDateTimeList(reader) : ReadDateTime(reader.Value);

		private static List<DateTime> ReadDateTimeList(JsonReader reader) =>
			JArray.Load(reader)
				.Select(item => ReadDateTime((item as JValue)?.Value))
				.Where(date => date != null)
				.Select(date => date!.Value)
				.ToList();

		/// <summary>
		/// Returns the value the reader has already parsed, keeping its <see cref="DateTimeKind"/> and
		/// sub-second precision intact, rather than round-tripping it through a culture-formatted string
		/// (which dropped the offset and fractional seconds). The `infinity` sentinels are still mapped
		/// to <see cref="DateTime.MaxValue"/> / <see cref="DateTime.MinValue"/>.
		/// </summary>
		private static DateTime? ReadDateTime(object? value) =>
			value switch
			{
				null => null,
				DateTime dateTime => dateTime,
				DateTimeOffset dateTimeOffset => dateTimeOffset.LocalDateTime,
				string text => ParseFromText(text),
				_ => ParseFromObject(value)
			};

		private static DateTime ParseFromObject(object value) => 
			DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

		private static DateTime ParseFromText(string text) => 
			ParseInfinity(text) ?? ParseFromObject(text);

		private static DateTime? ParseInfinity(string input) => 
			input.Contains("infinity") ? input.Contains("-") ? DateTime.MinValue : DateTime.MaxValue : null;

		/// <summary>
		/// Writes the value with its wall-clock intact, mirroring the read path. The default handling
		/// (via <c>DateTimeStyles.AdjustToUniversal</c>) forced every value through `ToUniversalTime()`,
		/// which shifts an <see cref="DateTimeKind.Unspecified"/> date to the previous day in timezones
		/// ahead of UTC and, for `date` columns, silently stored the wrong day.
		/// </summary>
		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			switch (value)
			{
				case null:
					writer.WriteNull();
					break;
				case DateTime dateTime:
					WriteDateTime(writer, dateTime);
					break;
				case IEnumerable<DateTime> dateTimes:
					writer.WriteStartArray();
					dateTimes.ToList().ForEach(date => WriteDateTime(writer, date));
					writer.WriteEndArray();
					break;
			}
		}

		/// <summary>
		/// Writes a single value, mapping <see cref="DateTime.MaxValue"/> back to the `infinity` sentinel
		/// the read path maps it from: Postgres rounds `MaxValue` up to year 10000 when stored as a literal
		/// timestamp, which then cannot be read back. <see cref="DateTime.MinValue"/> is left as a literal
		/// (it round-trips cleanly and doubles as the default for an unset value, so it is not `-infinity`).
		/// </summary>
		private static void WriteDateTime(JsonWriter writer, DateTime value)
		{
			if (value == DateTime.MaxValue)
				writer.WriteValue("infinity");
			else
				writer.WriteValue(value);
		}
	}
}
