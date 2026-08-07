using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Postgrest.Tests.Models;

[Table("foreign_key_test")]
public class ForeignKeyTestModel : BaseModel
{
    [PrimaryKey("id")] public int Id { get; set; }

    [Reference(typeof(Movie), foreignKey: "foreign_key_test_relation_1")]
    public Movie MovieFK1 { get; set; } = null!;

    [Reference(typeof(Movie), foreignKey: "foreign_key_test_relation_2")]
    public Movie MovieFK2 { get; set; } = null!;

    [Reference(typeof(Person))] public Person RandomPersonFK { get; set; } = null!;
}
