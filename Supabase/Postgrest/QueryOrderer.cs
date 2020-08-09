using System;
using static Supabase.Postgrest.Helpers;

namespace Supabase.Postgrest
{
    public class QueryOrderer
    {
        public string ForeignTable { get; set; }
        public string Column { get; set; }
        public Ordering Ordering { get; set; }
        public NullPosition NullPosition { get; set; }

        public QueryOrderer(string foreignTable, string column, Ordering ordering, NullPosition nullPosition)
        {
            ForeignTable = foreignTable;
            Column = column;
            Ordering = ordering;
            NullPosition = nullPosition;
        }
    }
}
