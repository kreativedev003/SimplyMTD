using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using SimplyMTD.Models.MTD;
using EllipticCurve.Utils;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;

namespace SimplyMTD.Pages
{
    public partial class AddClient
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
        public ClientService ClientService { get; set; }
		[Inject]
		public SecurityService securityService { get; set; }
		[Inject]
		public MTDService mtdService { get; set; }

		[Inject]
		public VATService VATService{ get; set; }
		[Inject]
		protected Blazored.SessionStorage.ISessionStorageService sessionStorage { get; set; }
		[Parameter]
        public UserDetail client { get; set; } = new UserDetail();
		[TempData]
		public string VRN { get; set; }
		public List<string> w8users { get; set; }

		protected bool isNew = true;
		protected bool isEdit = false;

        protected bool msgVisible;
        protected string msgContent;
        protected string msgTitle;
        protected AlertStyle msgStyle;
        protected string authorisation;
        protected string subscription;

		string[] authorisationList = { "Not Available", "Requested", "Authorised" };
        string[] subscriptionList = { "Active", "Overdue", "Expired" };
        string[] businessType = { "Sole-Trader/Self-Employed", "Partnership/LLP", "Limited Company", "Other" };

		protected override async Task OnInitializedAsync()
        {

            isNew = client.Id == null;
            isEdit = (isNew ? true : false);
           
            List<W8> data = await mtdService.GetW8UsersByUserId(securityService.User.Id);
            w8users = new List<string>();
            foreach(W8 user in data)
            {
                w8users.Add(user.Name);
            }

            authorisation = authorisationList[client.Authorisation];
			subscription = subscriptionList[client.Subscription];

            if(isNew)
                client.RegDate = DateTime.Now;
		}

        protected async Task AccessClientDashboard()
        {
			await sessionStorage.SetItemAsync<string>("client-vrn", client.Vrn);

			NavigationManager.NavigateTo("/agent/dashboard");
        }

        protected async Task Edit()
        {
            isEdit = true;
            StateHasChanged();
        }

        protected async Task CreateClient()
        {
            client.AgentId = securityService.User.Id;
            await ClientService.CreateClient(client);
            DialogService.Close();
        }

		protected async Task Lookup()
		{
			string _business_name, _reg_address;
			(_business_name, _reg_address) = await VATService.LookupVrn(client.Vrn);
            client.BusinessName = _business_name;
            client.Address = _reg_address;
            Console.WriteLine(_business_name);
			Console.WriteLine(_reg_address);
			StateHasChanged();
		}

		protected async Task SaveClient()
        {
            await ClientService.UpdateClient(client);
            DialogService.Close();
        }
        protected async Task DeleteClient()
        {
            await ClientService.DeleteClient(client);
            DialogService.Close();
        }
        protected async Task Cancel()
        {
            DialogService.Close();
        }

        protected async Task SendRequest()
        {
            msgVisible = false;
            msgContent = null;
            string message = await ClientService.SendRequest(client);
            if (!message.IsNullOrEmpty())
            {
                msgVisible = true;
                msgContent = message;
                msgTitle = "Send Request Error!";
                msgStyle = AlertStyle.Danger;
            } else
            {
                msgVisible = true;
                msgContent = "";
                msgTitle = "Send Request Success!";
                msgStyle = AlertStyle.Success;
                client.Authorisation = 1;
				authorisation = authorisationList[client.Authorisation];

			}
            StateHasChanged();
        }
        protected async Task CancelRequest()
        {
			msgVisible = false;
			msgContent = null;
			string message = await ClientService.CancelRequest(client);
			if (!message.IsNullOrEmpty())
			{
				msgVisible = true;
				msgContent = message;
				msgTitle = "Cancel Request Error!";
				msgStyle = AlertStyle.Danger;
			}
			else
			{
				msgVisible = true;
				msgContent = "";
				msgTitle = "Cancel Request Success!";
				msgStyle = AlertStyle.Success;
				client.Authorisation = 0;
                authorisation = authorisationList[client.Authorisation];
			}
			StateHasChanged();
		}
        protected async Task InformClient()
        {
            await DialogService.OpenAsync<InformationRequest>("Information Request", new Dictionary<string, object> { { "userDetail", client } },
                new DialogOptions() { Width = "900px", Height = "450px"});
        }
    }
}