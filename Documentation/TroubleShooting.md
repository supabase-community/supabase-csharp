
# Troubleshooting

**Q: I've created a User but while attempting to log in it throws an exception:**

A: Provided the credentials are correct, make sure that the User has also confirmed their email.

Adding a handler for email confirmation to a desktop or mobile application can be done, but it
requires setting up URL handlers for each platform, which can be pretty difficult to do if you
aren't really comfortable with configuring these handlers. (
e.g. [Windows](https://learn.microsoft.com/en-us/windows/win32/search/-search-3x-wds-ph-install-registration),
[Apple](https://developer.apple.com/documentation/xcode/defining-a-custom-url-scheme-for-your-app),
[Android](https://developer.android.com/training/app-links))
You may find it easier to create a
simple web application to handle email confirmation - that way a user can just click a link in
their email and get confirmed that way. Your desktop or mobile app should inspect the user object
that comes back and use that to see if the user is confirmed.

You might find it easiest to do something like create and deploy a
simple [SvelteKit](https://kit.svelte.dev/) or even a very basic
pure [JavaScript](https://github.com/supabase/examples-archive/tree/main/supabase-js-v1/auth/javascript-auth) project
to handle email verification.
