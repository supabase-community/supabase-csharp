#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Postgrest.Tests.Models
{
    [Table("kitchen_sink")]
    public class KitchenSink : BaseModel
    {
        [PrimaryKey("id")] public Guid? Id { get; set; }

        [Column("string_value")] public string? StringValue { get; set; }

        [Column("bool_value")] public bool BooleanValue { get; set; }

        [Column("unique_value")] public string? UniqueValue { get; set; }

        [Column("int_value")] public int? IntValue { get; set; }

        [Column("long_value")] public long? LongValue { get; set; }

        [Column("float_value")] public float FloatValue { get; set; }

        [Column("double_value")] public double DoubleValue { get; set; }

        [Column("datetime_value")] public DateTime? DateTimeValue { get; set; }

        [Column("datetime_value_1", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? DateTimeValue1 { get; set; }

        [Column("datetime_pos_infinite_value", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? DateTimePosInfinity { get; set; }

        [Column("datetime_neg_infinite_value", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? DateTimeNegInfinity { get; set; }

        [Column("list_of_strings")] public List<string>? ListOfStrings { get; set; }

        [Column("list_of_datetimes", NullValueHandling = NullValueHandling.Ignore)]
        public List<DateTime>? ListOfDateTimes { get; set; }

        [Column("list_of_ints")] public List<int>? ListOfInts { get; set; }

        [Column("list_of_floats")] public List<float>? ListOfFloats { get; set; }

        [Column("int_range")] public IntRange? IntRange { get; set; }

        [Column("uuidv4")] public Guid? Uuidv4 { get; set; }
    }
}
