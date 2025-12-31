using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Exceptions;

namespace StorageTests;

[TestClass]
public class ExceptionTests
{
    [TestMethod("Throws: Unauthorized Exception (No Authorization Header)")]
    public async Task ThrowsUnauthorizedExceptionNoAuthorization()
    {
        var noAuthorizationClient = new Client(Helpers.StorageUrl);
        
        var ex = await Assert.ThrowsExceptionAsync<SupabaseStorageException>(() =>
            noAuthorizationClient.CreateBucket("expected-to-fail"));
        
        Assert.IsTrue(ex.Reason == FailureHint.Reason.NotAuthorized);
    }
    
    [TestMethod("Throws Unauthorized Exception (Garbage Authorization)")]
    public async Task ThrowsUnauthorizedExceptionGarbageAuthorization()
    {
        var badAuthorization = new Client(Helpers.StorageUrl, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer GarbageKey"
        });
        
        var ex = await Assert.ThrowsExceptionAsync<SupabaseStorageException>(() =>
            badAuthorization.CreateBucket("expected-to-fail"));
        
        Assert.IsTrue(ex.Reason == FailureHint.Reason.NotAuthorized);
    }
    
    [TestMethod("Throws Unauthorized Exception (Bad Authorization)")]
    public async Task ThrowsUnauthorizedExceptionBadAuthorization()
    {
        var badAuthorization = new Client(Helpers.StorageUrl, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
        });
        
        var ex = await Assert.ThrowsExceptionAsync<SupabaseStorageException>(() =>
            badAuthorization.CreateBucket("expected-to-fail"));
        
        Assert.IsTrue(ex.Reason == FailureHint.Reason.NotAuthorized);
    }
}