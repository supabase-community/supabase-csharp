using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using Supabase.Core.Diagnostics;

namespace Supabase.Postgrest
{
    /// <summary>
    /// Diagnostics for the Postgrest client, exposed through <see cref="System.Diagnostics"/> so
    /// consumers can subscribe with the OpenTelemetry SDK using <see cref="PostgrestDiagnostics.SourceName"/>
    /// (<c>AddSource(...)</c> / <c>AddMeter(...)</c>). Emission is zero-cost when nothing is listening.
    ///
    /// Telemetry must never carry secrets or PII: URLs are recorded without their query string
    /// (Postgrest puts column filters and their values there) and no tag may contain row data,
    /// a credential, or other sensitive value.
    /// </summary>
    internal static class PostgrestInstrumentation
    {
        /// <summary>Tag key carrying the logical database operation (select/insert/update/…).</summary>
        internal const string OperationTag = "db.operation";

        internal static readonly ActivitySource Source =
            Instrumentation.CreateActivitySource(typeof(PostgrestInstrumentation).Assembly, PostgrestDiagnostics.SourceName);

        private static readonly Meter Meter =
            Instrumentation.CreateMeter(typeof(PostgrestInstrumentation).Assembly, PostgrestDiagnostics.SourceName);

        private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
            "supabase.postgrest.http.request.duration", "s", "Duration of HTTP requests sent by the Postgrest client.");

        /// <summary>
        /// Maps an HTTP method and the request flags to the logical Postgrest operation name.
        /// </summary>
        /// <param name="method">The HTTP method of the request.</param>
        /// <param name="isInsert">Whether the request is an insert.</param>
        /// <param name="isUpdate">Whether the request is an update.</param>
        /// <param name="isUpsert">Whether the request is an upsert.</param>
        internal static string ResolveOperation(HttpMethod method, bool isInsert, bool isUpdate, bool isUpsert)
        {
            if (isUpsert) return "upsert";
            if (isInsert) return "insert";
            if (isUpdate) return "update";
            if (method == HttpMethod.Delete) return "delete";
            if (method == HttpMethod.Head) return "count";
            return "select";
        }

        /// <summary>
        /// Starts the client span for an outgoing HTTP request, tagged per OpenTelemetry HTTP
        /// conventions with the sanitized (query-less) URL. Returns null when nothing is listening.
        /// </summary>
        /// <param name="method">The HTTP method of the request.</param>
        /// <param name="uri">The request URI, sanitized before tagging.</param>
        /// <param name="operation">The logical operation name, or null when not known.</param>
        internal static Activity? StartHttpActivity(HttpMethod method, Uri uri, string? operation)
        {
            var activity = Source.StartActivity($"{method.Method} {uri.AbsolutePath}", ActivityKind.Client)
                .SetHttpRequestTags(method.Method, uri);

            if (operation != null)
                activity?.SetTag(OperationTag, operation);

            return activity;
        }

        /// <summary>
        /// Records the request duration histogram for an HTTP request outcome.
        /// </summary>
        /// <param name="method">The HTTP method of the request.</param>
        /// <param name="uri">The request URI; only its host and path are recorded.</param>
        /// <param name="operation">The logical operation name, or null when not known.</param>
        /// <param name="statusCode">The HTTP response status code, or null if the request never got one.</param>
        /// <param name="errorType">The error classification, or null on success.</param>
        /// <param name="startTimestamp">The <see cref="Stopwatch.GetTimestamp"/> value captured before the request.</param>
        internal static void RecordRequest(HttpMethod method, Uri uri, string? operation, int? statusCode, string? errorType, long startTimestamp)
        {
            if (!RequestDuration.Enabled)
                return;

            var tags = new TagList
            {
                { "http.request.method", method.Method },
                { "server.address", uri.Host },
                { "url.path", uri.AbsolutePath }
            };

            if (operation != null)
                tags.Add(OperationTag, operation);

            if (statusCode.HasValue)
                tags.Add("http.response.status_code", statusCode.Value);

            if (errorType != null)
                tags.Add("error.type", errorType);

            RequestDuration.Record(GetElapsedSeconds(startTimestamp), tags);
        }

        private static double GetElapsedSeconds(long startTimestamp) =>
            (Stopwatch.GetTimestamp() - startTimestamp) / (double)Stopwatch.Frequency;
    }
}
