namespace BlazorWebAssemblySupabaseTemplate.Pages.Auth;

public partial class Login
{
    protected string Email {get; set;} = "cliente1@gmail.com";
    protected string Password {get; set;} = "senhasdadasdaasd";

    public async Task OnClickLogin()
    {
        await AuthService.Login(Email, Password);
        Snackbar.Add("Login successfull");
        NavigationManager.NavigateTo($"/");
    }
}

