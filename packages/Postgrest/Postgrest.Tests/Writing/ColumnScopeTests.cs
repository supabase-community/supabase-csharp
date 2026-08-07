using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Supabase.Postgrest;

namespace Postgrest.Tests.Writing;

/// <summary>
///     <c>Columns</c> restricts an update to the named columns via a projection. An entry that is not a model
///     column is rejected synchronously with an <see cref="ArgumentException" />.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ColumnScopeTests
{
    private const string BaseUrl = "http://localhost:54321/rest/v1";
    private readonly Client client = new(BaseUrl);

    [TestMethod]
    public void Columns_ShouldThrow_GivenAProjectionEntryThatIsNotAColumn()
    {
        var movie = new Movie { Name = "Test" };
        Action act = () => { _ = client.Table<Movie>().Columns(x => new object[] { "something", DateTime.Now }).Update(movie); };
        act.Should().Throw<ArgumentException>();
    }
}
