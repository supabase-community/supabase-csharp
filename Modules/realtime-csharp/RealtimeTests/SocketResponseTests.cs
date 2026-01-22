using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Realtime.Socket;

namespace RealtimeTests;

[TestClass]
public class SocketResponseTests
{
    [TestMethod("Error Response Included and Deserialized.")]
    public void SocketResponseIncludesError()
    {
        var responseWithError =
            "{\"columns\":[{\"name\":\"id\",\"type\":\"int8\"},{\"name\":\"details\",\"type\":\"text\"}],\"commit_timestamp\":\"2021-12-28T23:59:38.984538+00:00\",\"schema\":\"public\",\"table\":\"todos\",\"type\":\"UPDATE\",\"old_record\":{\"details\":\"previous test\",\"id\":12,\"user_id\":1},\"record\":{\"details\":\"test...\",\"id\":12,\"user_id\":1},\"errors\":[\"Error 413: Payload Too Large\"]}";
        var errorResponse = JsonConvert.DeserializeObject<SocketResponsePayload>(responseWithError);

        CollectionAssert.Contains(errorResponse?.Errors, "Error 413: Payload Too Large");

        var responseWithoutError =
            "{\"columns\":[{\"name\":\"id\",\"type\":\"int8\"},{\"name\":\"details\",\"type\":\"text\"}],\"commit_timestamp\":\"2021-12-28T23:59:38.984538+00:00\",\"schema\":\"public\",\"table\":\"todos\",\"type\":\"UPDATE\",\"old_record\":{\"details\":\"previous test\",\"id\":12,\"user_id\":1},\"record\":{\"details\":\"test...\",\"id\":12,\"user_id\":1},\"errors\": null}";
        var successResponse = JsonConvert.DeserializeObject<SocketResponsePayload>(responseWithoutError);

        Assert.IsNull(successResponse?.Errors);
    }
}