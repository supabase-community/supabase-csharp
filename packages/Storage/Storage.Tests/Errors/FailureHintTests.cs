using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage.Exceptions;
using Reason = Supabase.Storage.Exceptions.FailureHint.Reason;

namespace Storage.Tests.Errors;

/// <summary>
/// Covers <see cref="FailureHint.DetectReason"/>: how a failed response's status code and body map
/// onto a <see cref="FailureHint.Reason"/>. A missing body is always
/// <see cref="FailureHint.Reason.Unknown"/>, and several 400/403 codes only count as authorization
/// failures when the body names a specific cause.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class FailureHintTests
{
    [TestMethod]
    public void DetectReason_ShouldReturnUnknown_GivenNoContent() =>
        FailureHint.DetectReason(Failure(401, null)).Should().Be(Reason.Unknown);

    [TestMethod]
    public void DetectReason_ShouldReturnNotAuthorized_Given400MentioningAuthorization() =>
        FailureHint.DetectReason(Failure(400, "Missing authorization header")).Should().Be(Reason.NotAuthorized);

    [TestMethod]
    public void DetectReason_ShouldReturnNotAuthorized_Given400Malformed() =>
        FailureHint.DetectReason(Failure(400, "malformed jwt")).Should().Be(Reason.NotAuthorized);

    [TestMethod]
    public void DetectReason_ShouldReturnNotAuthorized_Given400InvalidSignature() =>
        FailureHint.DetectReason(Failure(400, "invalid signature")).Should().Be(Reason.NotAuthorized);

    [TestMethod]
    public void DetectReason_ShouldReturnInvalidInput_Given400OtherwiseInvalid() =>
        FailureHint.DetectReason(Failure(400, "invalid input syntax")).Should().Be(Reason.InvalidInput);

    [TestMethod]
    public void DetectReason_ShouldReturnUnknown_Given400WithoutKnownCause() =>
        FailureHint.DetectReason(Failure(400, "bad request")).Should().Be(Reason.Unknown);

    [TestMethod]
    public void DetectReason_ShouldReturnNotAuthorized_Given401() =>
        FailureHint.DetectReason(Failure(401, "nope")).Should().Be(Reason.NotAuthorized);

    [TestMethod]
    public void DetectReason_ShouldReturnNotAuthorized_Given403InvalidCompactJws() =>
        FailureHint.DetectReason(Failure(403, "invalid compact JWS")).Should().Be(Reason.NotAuthorized);

    [TestMethod]
    public void DetectReason_ShouldReturnNotAuthorized_Given403SignatureVerificationFailed() =>
        FailureHint.DetectReason(Failure(403, "signature verification failed")).Should().Be(Reason.NotAuthorized);

    [TestMethod]
    public void DetectReason_ShouldReturnUnknown_Given403WithoutKnownCause() =>
        FailureHint.DetectReason(Failure(403, "forbidden")).Should().Be(Reason.Unknown);

    [TestMethod]
    public void DetectReason_ShouldReturnNotFound_Given404NotFound() =>
        FailureHint.DetectReason(Failure(404, "Object not found")).Should().Be(Reason.NotFound);

    [TestMethod]
    public void DetectReason_ShouldReturnAlreadyExists_Given409Exists() =>
        FailureHint.DetectReason(Failure(409, "The resource already exists")).Should().Be(Reason.AlreadyExists);

    [TestMethod]
    public void DetectReason_ShouldReturnEntityTooLarge_Given413() =>
        FailureHint.DetectReason(Failure(413, "<html>413 Request Entity Too Large</html>")).Should()
            .Be(Reason.EntityTooLarge,
                "a 413 is the oversized-upload signal that should steer callers to a resumable upload (issue #14)");

    [TestMethod]
    public void DetectReason_ShouldReturnInternal_Given500() =>
        FailureHint.DetectReason(Failure(500, "boom")).Should().Be(Reason.Internal);

    [TestMethod]
    public void DetectReason_ShouldReturnUnknown_GivenUnmappedStatus() =>
        FailureHint.DetectReason(Failure(418, "teapot")).Should().Be(Reason.Unknown);

    private static SupabaseStorageException Failure(int statusCode, string? content) =>
        new("failed") { StatusCode = statusCode, Content = content };
}
