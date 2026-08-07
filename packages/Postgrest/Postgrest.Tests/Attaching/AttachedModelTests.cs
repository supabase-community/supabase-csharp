using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Tests.Models;
using Postgrest.Tests.Support;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;

namespace Postgrest.Tests.Attaching;

/// <summary>
///     End-to-end proof that a model hydrated via <c>Client.Attach</c> carries enough context to issue its own
///     writes: update and delete reach the server rather than failing with the "BaseUrl should be set" guard.
/// </summary>
[TestClass]
[TestCategory("E2E")]
public class AttachedModelTests
{
    private static Movie AttachedMovie() => LocalStack.Client()
        .Attach(new Movie { Id = "11111111-1111-1111-1111-111111111111", Name = "Test" });

    [TestMethod]
    public async Task Update_ShouldNotThrowTheBaseUrlGuard_GivenAnAttachedModel()
    {
        try
        {
            await AttachedMovie().Update<Movie>();
        }
        catch (PostgrestException exception)
        {
            exception.Message.Should().NotContain("should be set in the model");
        }
    }

    [TestMethod]
    public async Task Delete_ShouldNotThrowTheBaseUrlGuard_GivenAnAttachedModel()
    {
        try
        {
            await AttachedMovie().Delete<Movie>();
        }
        catch (PostgrestException exception)
        {
            exception.Message.Should().NotContain("should be set in the model");
        }
    }
}
