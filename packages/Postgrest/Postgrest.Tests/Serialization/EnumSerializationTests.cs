using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Postgrest.Tests.Models;
using Supabase.Postgrest;

namespace Postgrest.Tests.Serialization;

/// <summary>
///     How the serializer renders enum-typed columns: integer underlying value by default, string name when
///     <see cref="ClientOptions.SerializeEnumsAsStrings" /> is on, and always an explicit <c>[EnumMember]</c>
///     mapping when the enum carries its own type-level <c>[JsonConverter]</c>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class EnumSerializationTests
{
    private readonly Movie movie = new() { Name = "Reservoir Dogs", Status = MovieStatus.OffDisplay };
    private readonly Todo todo = new() { UserId = 1, Status = Todo.TodoStatus.IN_PROGRESS };

    private static string Serialize(object model, bool serializeEnumsAsStrings = false) =>
        JsonConvert.SerializeObject(model,
            Client.SerializerSettings(new ClientOptions { SerializeEnumsAsStrings = serializeEnumsAsStrings }));

    [TestMethod]
    public void Write_ShouldSerializeAsUnderlyingInteger_GivenSerializeEnumsAsStringsOff() =>
        Serialize(movie).Should().Contain("\"status\":1");

    [TestMethod]
    public void Write_ShouldSerializeAsStringName_GivenSerializeEnumsAsStringsOn() =>
        Serialize(movie, serializeEnumsAsStrings: true).Should().Contain("\"status\":\"OffDisplay\"");

    [TestMethod]
    public void Write_ShouldRoundTrip_GivenSerializeEnumsAsStringsOn()
    {
        var settings = Client.SerializerSettings(new ClientOptions { SerializeEnumsAsStrings = true });
        var json = JsonConvert.SerializeObject(movie, settings);
        JsonConvert.DeserializeObject<Movie>(json, settings)!.Status.Should().Be(MovieStatus.OffDisplay);
    }

    [TestMethod]
    public void Write_ShouldHonorEnumMemberMapping_GivenTypeLevelJsonConverter() =>
        Serialize(todo).Should().Contain("\"status\":\"IN PROGRESS\"");
}
