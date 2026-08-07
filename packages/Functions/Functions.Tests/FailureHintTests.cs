using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Functions.Exceptions;
using Reason = Supabase.Functions.Exceptions.FailureHint.Reason;

namespace Functions.Tests
{
    /// <summary>
    /// Covers <see cref="FailureHint.DetectReason"/>: the mapping from a failed response's status code
    /// and content onto a <see cref="FailureHint.Reason"/>. A missing body is always
    /// <see cref="FailureHint.Reason.Unknown"/>, and 403 only counts as an authorization failure when
    /// the body mentions an API key.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class FailureHintTests
    {
        [TestMethod]
        public void DetectReason_ShouldReturnUnknown_GivenNoContent() =>
            FailureHint.DetectReason(Failure(statusCode: 401, content: null)).Should().Be(Reason.Unknown);

        [TestMethod]
        public void DetectReason_ShouldReturnNotAuthorized_Given401() =>
            FailureHint.DetectReason(Failure(statusCode: 401, content: "nope")).Should().Be(Reason.NotAuthorized);

        [TestMethod]
        public void DetectReason_ShouldReturnNotAuthorized_Given403MentioningApiKey() =>
            FailureHint.DetectReason(Failure(statusCode: 403, content: "invalid apikey")).Should().Be(Reason.NotAuthorized);

        [TestMethod]
        public void DetectReason_ShouldReturnUnknown_Given403WithoutApiKey() =>
            FailureHint.DetectReason(Failure(statusCode: 403, content: "forbidden")).Should().Be(Reason.Unknown);

        [TestMethod]
        public void DetectReason_ShouldReturnInternal_Given500() =>
            FailureHint.DetectReason(Failure(statusCode: 500, content: "boom")).Should().Be(Reason.Internal);

        [TestMethod]
        public void DetectReason_ShouldReturnUnknown_GivenUnmappedStatus() =>
            FailureHint.DetectReason(Failure(statusCode: 400, content: "bad request")).Should().Be(Reason.Unknown);

        private static FunctionsException Failure(int statusCode, string? content) =>
            new("failed") { StatusCode = statusCode, Content = content };
    }
}
