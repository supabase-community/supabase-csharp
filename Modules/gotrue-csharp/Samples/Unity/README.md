
To use supabase-csharp with Unity, you will want to install the following:

- UniTask, via Package Manager. Use https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask as the Git URL. Version 2.3.3 as of this writing. This package is required to support async/await integration in Unity.
- Newtonsoft Json, via Package Manager. Use Add Package by Name, and enter com.unity.nuget.newtonsoft-json as the name. Version 3.2.1 as of this writing. You must use this version and not the version available on npm as this version is heavily customized by Unity to work property with IL2CPP.

You will also need to install the following .NET Standard 2.0 DLLs:

- MimeMapping
- System.Reactive
- System.Threading.Channels
- System.Threading.Tasks.Extensions
- Websocket.Client

You can download these directly from the npm directory, or you can grab the supporting-dlls.zip file in this directory.

The UnitySession.cs provides an implementation of GoTrue session persistence compatible with
Unity. Note that PlayerPrefs.SetString won't work, as it fails if not called from
the main UI thread.
