using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Postgrest.Tests.Models;

[Table("nested_foreign_key_test")]
public class NestedForeignKeyTestModel : BaseModel
{
    [PrimaryKey("id")] public int Id { get; set; }

    [Reference(typeof(ForeignKeyTestModel))]
    public ForeignKeyTestModel FKTestModel { get; set; } = null!;

    [Reference(typeof(User))] public User User { get; set; } = null!;
}
