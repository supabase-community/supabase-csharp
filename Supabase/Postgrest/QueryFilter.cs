using System;
using static Supabase.Postgrest.Helpers;

namespace Supabase.Postgrest
{

    public class QueryFilter
    {
        public string Property { get; set; }
        public Operator Op { get; set; }
        public string Criteria { get; set; }

        public QueryFilter(string property, Operator op, string criteria)
        {
            Property = property;
            Op = op;
            Criteria = criteria;
        }
    }
}
