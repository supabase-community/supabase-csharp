using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Supabase.Postgrest.Extensions;
using Supabase.Postgrest.Exceptions;

namespace Supabase.Postgrest.Converters
{

	/// <summary>
	/// Used by Newtonsoft.Json to convert a C# range into a Postgrest range.
	/// </summary>
	internal class RangeConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			throw new NotImplementedException();
		}

		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			return reader.Value != null ? ParseIntRange(reader.Value.ToString()) : null;
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			if (value == null) return;

			var val = (IntRange)value;
			writer.WriteValue(val.ToPostgresString());
		}

		public static IntRange ParseIntRange(string value)
		{
			//int4range (0,1] , [123,4123], etc. etc.
			const string pattern = @"^(\[|\()(\d+),(\d+)(\]|\))$";
			var matches = Regex.Matches(value, pattern);

			if (matches.Count <= 0)
				throw new PostgrestException("Unknown Range format.") { Reason = FailureHint.Reason.InvalidArgument };

			var groups = matches[0].Groups;
			var isInclusiveLower = groups[1].Value == "[";
			var isInclusiveUpper = groups[4].Value == "]";
			var value1 = int.Parse(groups[2].Value);
			var value2 = int.Parse(groups[3].Value);

			var start = isInclusiveLower ? value1 : value1 + 1;
			var count = isInclusiveUpper ? value2 : value2 - 1;

			// Edge-case, includes no points
			return count < start ? new IntRange(0, 0) : new IntRange(start, count);
		}
	}
}
