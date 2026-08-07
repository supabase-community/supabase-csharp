using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Supabase.Postgrest;

namespace Postgrest.Tests.Writing;

/// <summary>
///     <c>OnConflict</c> names the single column an upsert resolves against. Passing an expression that is not
///     a single model column — a projection of several, or a bare string — is rejected synchronously with an
///     <see cref="ArgumentException" />.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class OnConflictTests
{
    private const string BaseUrl = "http://localhost:54321/rest/v1";
    private readonly Client client = new(BaseUrl);

    [TestMethod]
    public void OnConflict_ShouldThrow_GivenMultipleColumns()
    {
        var act = () => client.Table<User>().OnConflict(x => new object[] { x.Username!, x.FavoriteName! });
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void OnConflict_ShouldThrow_GivenAnExpressionThatIsNotAColumn()
    {
        var act = () => client.Table<User>().OnConflict(x => "something");
        act.Should().Throw<ArgumentException>();
    }
}
