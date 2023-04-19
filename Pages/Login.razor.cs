using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using System;

namespace SimplyMTD.Pages
{
    public partial class Login
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

		[Inject]
		protected Blazored.SessionStorage.ISessionStorageService sessionStorage { get; set; }

		protected string redirectUrl;
        protected string error;
        protected string info;
        protected bool errorVisible;
        protected bool infoVisible;

        [Inject]
        protected SecurityService Security { get; set; }
        protected int accountType = 1;

        protected override async Task OnInitializedAsync()
        {
            var query = System.Web.HttpUtility.ParseQueryString(new Uri(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).ToString()).Query);

            error = query.Get("error");

            info = query.Get("info");

            redirectUrl = query.Get("redirectUrl");

            errorVisible = !string.IsNullOrEmpty(error);

            infoVisible = !string.IsNullOrEmpty(info);

            //check uri if it is regarding to register;
            
            
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
		}

        protected async Task Register()
        {
            NavigationManager.NavigateTo("/register");
            /*
            var result = await DialogService.OpenAsync<RegisterApplicationUser>("Register Application User");

            if (result == true)
            {
                infoVisible = true;

                info = "Registration accepted. Please check your email for further instructions.";
            }
            */
        }

        protected async Task ResetPassword()
        {
            var result = await DialogService.OpenAsync<ResetPassword>("Reset password");

            if (result == true)
            {
                infoVisible = true;

                info = "Password reset successfully. Please check your email for further instructions.";
            }
        }

        protected async System.Threading.Tasks.Task Submit(string args)
        {
            Console.WriteLine(args);
		}
	}
}