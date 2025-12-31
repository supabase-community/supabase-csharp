using System;

namespace Supabase.Storage.Interfaces
{
    internal interface IProgressableStreamContent
    {
        IProgress<float>? Progress { get; }
    }
}