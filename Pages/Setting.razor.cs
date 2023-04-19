using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;
using SendGrid.Helpers.Mail;
using SendGrid;
using SimplyMTD.Models;
using SimplyMTD.Models.MTD;
using System;

namespace SimplyMTD.Pages
{
    public partial class Setting
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
		public IConfiguration configuration { get; set; }

		protected string oldPassword = "";
        protected string newPassword = "";
        protected string confirmPassword = "";
        
        protected string error;
        protected bool errorVisible;
        protected bool successVisible;

		protected string agentName { get; set; } = "";
		protected string agentEmail { get; set; } = "";

		[Inject]
        protected SecurityService Security { get; set; }

		[Inject]
		protected ClientService clientService { get; set; }

		protected SimplyMTD.Models.ApplicationUser user;
        [Inject]
        public MTDService MTDService { get; set; }

		RadzenUpload upload;

		protected string PhotoUrl;

		protected UserDetail userDetail;

		protected Billing billing;

		protected Accounting accounting;

		protected Accountant accountant;

		protected IEnumerable<W8> w8users;

		protected RadzenDataGrid<W8> gridW8;

		protected override async Task OnInitializedAsync()
        {
			//user = await Security.GetUserById($"{Security.User.Id}");
			user = await MTDService.GetUserById($"{Security.User.Id}");
			// accounting = await MTDService.GetAccountingByUserId("12");
			//var test = user.UserDetail.ClientId;
			userDetail = await MTDService.GetUserDetailByUserId(user.Id);
			billing = await MTDService.GetBillingByUserId(user.Id);
			accounting = await MTDService.GetAccountingByUserId(user.Id);
			accountant = await MTDService.GetAccountantByUserId(user.Id);
			w8users = await MTDService.GetW8UsersByUserId(user.Id);

			Console.WriteLine(w8users);
            Console.WriteLine(w8users.ToList().Count);
        }

		protected async Task w8Update(W8 user)
		{
			await DialogService.OpenAsync<AddW8>("Update User", new Dictionary<string, object> { { "w8user", user } });
			w8users = await MTDService.GetW8UsersByUserId(Security.User.Id);
			StateHasChanged();
		}

		protected async Task w8Create()
		{
			await DialogService.OpenAsync<AddW8>("Create User", new Dictionary<string, object> {});
			w8users = await MTDService.GetW8UsersByUserId(Security.User.Id);
			StateHasChanged();
		}

		protected async Task w8Delete(W8 user)
		{
			await clientService.DeleteW8(user);
			w8users = await MTDService.GetW8UsersByUserId(Security.User.Id);
			StateHasChanged();
		}

		protected async Task GeneralFormSubmit(SimplyMTD.Models.MTD.UserDetail userDetail)
		{
			try
			{
				// bool res = await Security.UpdateUser1(user);

				bool res = await MTDService.UpdateUserDetail(userDetail.Id, userDetail);
				if (res)
				{
					ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Success", Detail = "", Duration = 4000 });
				}
				else
				{
					ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "", Duration = 4000 });
				}

			}
			catch (Exception ex)
			{
				errorVisible = true;
				error = ex.Message;
			}
		}

		protected async Task BillingFormSubmit()
		{
			try
			{
				//billing.UserId = user.Id;
				await MTDService.UpdateBilling(user.Id, billing);

			}
			catch (Exception ex)
			{
				errorVisible = true;
				error = ex.Message;
			}
		}

		protected async Task AccountingFormSubmit()
		{
			try
			{
				await MTDService.UpdateAccounting(user.Id, accounting);

			}
			catch (Exception ex)
			{
				errorVisible = true;
				error = ex.Message;
			}
		}

		protected async Task AccountantFormSubmit()
		{
			try
			{
				await MTDService.UpdateAccountant(user.Id, accountant);

			}
			catch (Exception ex)
			{
				errorVisible = true;
				error = ex.Message;
			}
		}

		protected async Task ChangePasswordFormSubmit()
		{
			try

			{
				await Security.ChangePassword(oldPassword, newPassword);
				successVisible = true;
			}

			catch (Exception ex)
			{
				errorVisible = true;
				error = ex.Message;
			}
		}

		protected async Task FormSubmit()
		{

		}

		void Cancel()
        {
            //
        }

		void OnProgress(UploadProgressArgs args, string name)
		{
			Console.WriteLine(name);
			if (args.Progress == 100)
			{
				foreach (var file in args.Files)
				{
				}
			}
		}

		void OnComplete(UploadCompleteEventArgs args)
		{
			var test = args.JsonResponse;
			PhotoUrl = JsonConvert.DeserializeObject<UploadRes>(args.RawResponse).Url;
/*			user.Photo = PhotoUrl;
*/		}

		void OnChange(UploadChangeEventArgs args, string name)
		{
			foreach (var file in args.Files)
			{
			}

		}

		void ShowNotification(NotificationMessage message)
		{
			NotificationService.Notify(message);

		}

		/*public class Test
		{
			public int TaxYear { get; set; }
		}

		Test test = new Test()
		{

		};*/

		public class Basis
		{
			public string Id { get; set; }
			public string Name { get; set; }
		}

		List<Basis> basis = new List<Basis>()
		{
			new Basis() { Id = "PB", Name = "Payment Based" },
			new Basis() { Id = "IB", Name = "Invoice Based" },
			new Basis() { Id = "FRPB", Name = "Flat Rate - Payment Based" },
			new Basis() { Id = "FRIB", Name = "Flat Rate - Invoice Based" },
			new Basis() { Id = "NOVAT", Name = "Not VAT Registered" },
		};

		IEnumerable<int> values = new int[] { 1 };

		List<Basis> businessTypes = new List<Basis>()
		{
			new Basis() { Id = "Sole Trader", Name = "Sole Trader" },
			new Basis() { Id = "Small Business Limited Company", Name = "Small Business Limited Company" },
			new Basis() { Id = "LLP Partnership", Name = "LLP Partnership" },
		};

		public class UploadRes
		{
			public string Url { get; set; }
		}

		public async Task InviteAgent()
		{
			if (agentEmail.IsNullOrEmpty())
				return;
			UserDetail agent = await MTDService.GetUserDetailByEmail(agentEmail);
			if (agent == null) return;

			userDetail.AgentEmail = agentEmail;
			userDetail.AgentName = agentName;
			userDetail.AgentId = agent.UserId;

			await MTDService.UpdateUserDetail(userDetail.Id, userDetail);


			string sendGridApiKey = configuration.GetValue<string>("Sendgrid:API_KEY");
			if (string.IsNullOrEmpty(sendGridApiKey))
			{
				throw new Exception("The 'SendGridApiKey' is not configured");
			}

			var client = new SendGridClient(sendGridApiKey);
			var msg = new SendGridMessage()
			{
				From = new EmailAddress(configuration.GetValue<string>("Sendgrid:FROM_EMAIL"), "Agent Access Detail"),
				Subject = "Agent Access Invitation",
				PlainTextContent = string.Format("Agent Access Detail"),
				HtmlContent = string.Format("Agent Access Detail")
			};
			msg.AddTo(new EmailAddress(agentEmail));
			var response = await client.SendEmailAsync(msg);
		}

		public async Task CancelAgent()
		{
			userDetail.AgentId = null;
			await MTDService.UpdateUserDetail(userDetail.Id, userDetail);
		}
	}
}