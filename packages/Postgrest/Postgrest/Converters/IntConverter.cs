using System;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace Supabase.Postgrest.Converters
{

	/// <inheritdoc />
	public class IntArrayConverter : JsonConverter
	{
		/// <inheritdoc />
		public override bool CanConvert(Type objectType)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public override bool CanRead => false;

		/// <inheritdoc />
		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			if (value is List<int> list)
			{
				writer.WriteValue($"{{{string.Join(",", list)}}}");
			}
		}
	}
}
