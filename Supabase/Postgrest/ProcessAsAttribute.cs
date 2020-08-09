using System;
namespace Supabase.Postgrest
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ProcessAsAttribute : Attribute
    {
        public string Operator { get; set; }
        public string Formatter { get; set; }

        public ProcessAsAttribute(string @operator, string formatter = null)
        {
            Operator = @operator;
            Formatter = formatter;
        }
    }
}
