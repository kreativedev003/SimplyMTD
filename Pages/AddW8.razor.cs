using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace SimplyMTD.Pages
{
    public partial class AddW8
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
        public MTDService MTDService { get; set; }

		[Inject]
		public ClientService clientService { get; set; }

		[Parameter] public SimplyMTD.Models.MTD.W8 w8user { get; set; } = new SimplyMTD.Models.MTD.W8();

        protected bool isNew = true;

        protected bool p1=false, p2=false, p3=false;

		protected override async Task OnInitializedAsync()
        {
            isNew = w8user.ClientId == null;
        }
        protected bool errorVisible;
		

		protected async Task FormSubmit()
        {
            try
            {
                Console.Write(w8user.Permission);

                if(isNew)
                {
					await clientService.CreateW8(w8user);
				} else
                {
					await clientService.UpdateW8(w8user);
				}
                DialogService.Close();
            }
            catch (Exception ex)
            {
                hasChanges = ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException;
                canEdit = !(ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException);
                errorVisible = true;
            }
        }

        protected async Task CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }


        protected bool hasChanges = false;
        protected bool canEdit = true;
    }
}