using System;
using System.Collections.Generic;

namespace StorageTests
{
    public static class Helpers
    {
        public static string SupabaseUrl => "http://127.0.0.1:54321/storage/v1";
        public static string PublicKey => "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";
        public static string ServiceKey => "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU";

        public static string StorageUrl => $"{SupabaseUrl}";
        
        public static Supabase.Storage.Client GetServiceClient()
        {
            return new Supabase.Storage.Client(StorageUrl, new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {ServiceKey}" },
            });
        }
        
        public static Supabase.Storage.Client GetPublicClient()
        {
            return new Supabase.Storage.Client(StorageUrl, new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {PublicKey}" },
            });
        }
    }
}

