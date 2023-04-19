using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using System;

namespace SimplyMTD.Pages
{
    public partial class Register
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
		protected SecurityService SecurityService { get; set; }

		[Inject]
		protected Blazored.SessionStorage.ISessionStorageService sessionStorage { get; set; }

		[Inject]
		protected SecurityService Security { get; set; }

		protected string redirectUrl;
        protected string error;
        protected string info;
        protected bool errorVisible;
        protected bool infoVisible;
        protected int currentTab { get; set; } = 1;

        protected string first_name { get; set; } = "";
		protected string last_name { get; set; } = "";
		protected string email { get; set; } = "";
		protected string password { get; set; } = "";


		protected void onIndividual()
        {
            Console.WriteLine("Individual");

            currentTab = 1;
        }

		protected void onAccountant()
		{
            currentTab = 2;
		}

        protected async Task onRegister()
        {
            try
            {
				await SecurityService.Register(first_name + " " + last_name, password, email, currentTab);
				infoVisible = true;
				info = "Registration accepted. Please check your email for further instructions.";
			}
			catch (Exception ex)
			{
				errorVisible = true;
				error = ex.Message;
			}
			
        }
	}
}