using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Supabase
{
    public static class Util
    {
        public static string GetAssemblyVersion()
        {
            var assembly = typeof(Supabase.Client).Assembly;
            var informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var name = assembly.GetName().Name;

            return $"{name.ToString().ToLower()}-csharp/{informationVersion}";
        }
    }
}
