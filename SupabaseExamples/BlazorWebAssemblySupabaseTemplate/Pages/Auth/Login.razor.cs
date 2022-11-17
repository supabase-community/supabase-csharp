namespace BlazorWebAssemblySupabaseTemplate.Pages.Auth;

public partial class Login
{
    protected string email {get; set;} = "cliente1@gmail.com";
    protected string password {get; set;} = "senhasdadasdaasd";

    public async Task OnClickLogin()
    {
        await AuthService.Login(email, password);
        Snackbar.Add("Login successfull");
        NavigationManager.NavigateTo($"/");
    }
}

