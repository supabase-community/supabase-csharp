#TODO
    - CustomSupabaseSessionHandler not working.
        - how to test: 
            - Logout
            - Login
            - Access Crud table private RLS page
            - Insert some row
            - you will see the row displayed below
            - press F5 on the browser
            - the rows will disapear
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