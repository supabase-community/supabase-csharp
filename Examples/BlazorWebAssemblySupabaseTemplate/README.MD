#TODO
    - CustomSupabaseSessionHandler not working.
        - how to test: 
            - Logout
            - Login
            - Access Crud table private RLS page
            - Insert some row
            - you will see the row displayed below
            - press F5 on the browser
            - the rows will disappear
        - Maybe i created CustomSupabaseSessionHandler wrong. But it seem like that the token is not being sent with the headers...

    - create policy to insert that check if the user_id is the same of the user logged in trying to insert.

# Credits
    https://github.com/supabase-community/supabase-csharp
    https://github.com/patrickgod/BlazorAuthenticationTutorial
    https://github.com/d11-jwaring/SupabaseRealtimeBlazorWASM/tree/master
    

# How to deploy
    dotnet publish -c Release -o release
    firebase deploy

# Error message
    Failed to find a valid digest in the 'integrity' attribute for resource 'https://blazorwasmsupabasetemplate.web.app/_framework/blazor.boot.json' with computed SHA-256 integrity 'XdcujrjLMAFyEwhjckKrX5naw+S/ieI/g8U7BkEVUc8='. The resource has been blocked.
    Unknown error occurred while trying to verify integrity.
    service-worker.js:22 Uncaught (in promise) TypeError: Failed to fetch
        at service-worker.js:22:54
        at async onInstall (service-worker.js:22:5)

    -----> This is because of old files in cache in the browser. Clear cache by clicking in the clear button (just ctrl + f5 doesn't work) and after press ctrl + f5. This will solve.



# JWT ERROR
    - WHEN CLICK IN LOGOUT BUTTON OR TRY TO GET DATA FROM DATABASE, BUT THE JWT IS ALREADY EXPIRED:
        - this problem only happens when login, close the app, later, after token expired, the app is open again.
        - I set the JWT expiry limit to 180 just to test this.

        blazor.webassembly.js:1 crit: Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]
            Unhandled exception rendering component: {"code":401,"msg":"invalid JWT: unable to parse or verify signature, token is expired by 22m22s"}
        Supabase.Gotrue.RequestException: {"code":401,"msg":"invalid JWT: unable to parse or verify signature, token is expired by 22m22s"}
        at Supabase.Gotrue.Helpers.MakeRequest(HttpMethod method, String url, Object data, Dictionary`2 headers)
        at Supabase.Gotrue.Client.SignOut()
        at BlazorWebAssemblySupabaseTemplate.Services.AuthService.Logout()
        at BlazorWebAssemblySupabaseTemplate.Shared.MainLayout.OnClickLogout()
        at Microsoft.AspNetCore.Components.ComponentBase.CallStateHasChangedOnAsyncCompletion(Task task)
        at MudBlazor.MudChip.OnClickHandler(MouseEventArgs ev)
        at Microsoft.AspNetCore.Components.ComponentBase.CallStateHasChangedOnAsyncCompletion(Task task)
        at Microsoft.AspNetCore.Components.RenderTree.Renderer.GetErrorHandledTask(Task , ComponentState )


