using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using Microsoft.OData;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;
using SimplyMTD.Data;
using SimplyMTD.Models;
using SimplyMTD.Models.MTD;
using static System.Net.WebRequestMethods;
using FileInfo = Radzen.FileInfo;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace SimplyMTD.Pages
{
	public partial class AgentDashboard
	{
		[Inject]
		protected IJSRuntime JSRuntime { get; set; }

		[Inject]
		protected NavigationManager UriHelper { get; set; }

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
		protected SecurityService Security { get; set; }

		[Inject]
		public VATService VATService { get; set; }

		[Inject]
		public TokenProvider tokenProvider { get; set; }

		[Inject]
		public MTDService MTDSevice { get; set; }
		
		[Inject]
		public MTDContext MTDContext { get; set; }

		[Inject]
		public SecurityService SecurityService { get; set; }

		[Inject]
		HttpClient SimplyMTDHttpClient { get; set; }

		[Inject]
		protected Blazored.SessionStorage.ISessionStorageService sessionStorage { get; set; }

		[Inject]
        public MTDService MTDService { get; set; }
		
		[Inject]
		public IHttpClientFactory factory { get; set; }

		protected IEnumerable<Obligation> obligations;

		protected Dictionary<string, string> attachments = new Dictionary<string, string>();

		protected IEnumerable<Liability> liabilities;

		protected IEnumerable<Payment> payments;

		protected IEnumerable<UserDetail> clients;

		protected List<UserDetail> all_clients;

		protected IList<UserDetail> selectedClients;

		protected RadzenDataGrid<Obligation> grid1;

		protected RadzenDataGrid<Liability> grid2;

		protected RadzenDataGrid<Payment> grid3;

		protected RadzenDataGrid<UserDetail> grid4;

		protected SimplyMTD.Models.ApplicationUser user;

		protected SimplyMTD.Models.MTD.UserDetail userDetail;

		protected int accountType = 0;

		protected string search_string = "";

		protected string clientVrn = "";

		protected override async Task OnInitializedAsync()
		{
			
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if(firstRender)
			{
				// accountType = await sessionStorage.GetItemAsync<int>("accountType");
				clientVrn = await sessionStorage.GetItemAsync<string>("client-vrn");
				accountType = SecurityService.userDetail.Type;
				StateHasChanged();
                await Load();
			}
		}

		void ShowNotification(NotificationMessage message)
		{
			NotificationService.Notify(message);
		}

		protected async System.Threading.Tasks.Task Load()
		{
			obligations = await VATService.GetObligations(clientVrn);
			if (obligations != null)
			{
				foreach (var obligation in obligations)
				{
					if (obligation.status == "O")
					{
						// update the client 
						user = await Security.GetUserById($"{Security.User.Id}");
						userDetail = await MTDService.GetUserDetailByUserId($"{Security.User.Id}");
						userDetail.Start = DateTime.Parse(obligation.start);
						userDetail.End = DateTime.Parse(obligation.end);
						userDetail.Deadline = DateTime.Parse(obligation.due);
						await MTDService.UpdateUserDetail(userDetail.Id, userDetail);
						/*user.Start = DateTime.Parse(obligation.start);
						user.End = DateTime.Parse(obligation.end);
						user.Deadline = DateTime.Parse(obligation.due);*/
					}
					var attachment = await MTDService.GetAttachmentByPeriodKey(obligation.periodKey);
					attachments.Add(obligation.periodKey, attachment);
					obligation.attachment = !(attachment.IsNullOrEmpty());
					StateHasChanged();
				}
			}
			liabilities = await VATService.GetLiabilities(clientVrn);
			StateHasChanged();
			payments = await VATService.GetPayments(clientVrn);
			StateHasChanged();
		}

		public string selectedPeriodKey { get; set; } = "";
		public int selectedIndex { get; set; }

		public async Task ChangeAttach(bool state, Obligation data)
		{
			selectedPeriodKey = data.periodKey;
			selectedIndex = obligations.ToList().IndexOf(data);

			if (state == true)
			{
				var fileInfo = await JSRuntime.InvokeAsync<FileInfo>("showOpenFileDialog");
			}
			else
			{
				string periodKey = data.periodKey;
				await MTDService.removeAttach(periodKey);
				attachments.Remove(periodKey);
				obligations.ToList()[selectedIndex].attachment = false;
			}
		}

		private string GenerateRandomName()
		{
			DateTime now = DateTime.Now;
			string dateTimeString = now.ToString("yyyyMMddHHmmssfff");
			byte[] bytes = Encoding.UTF8.GetBytes(dateTimeString);
			using var md5 = System.Security.Cryptography.MD5.Create();
			byte[] hash = md5.ComputeHash(bytes);

			string hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()+".xlsx";
			return hashString;
		}
		private async Task UploadFile(IBrowserFile file)
		{
			using var formData = new MultipartFormDataContent();
			using var httpClient = factory.CreateClient("SimplyMTD");
			var fileName = GenerateRandomName();
			formData.Add(new StreamContent(file.OpenReadStream()), "file", fileName);
			formData.Add(new StringContent(selectedPeriodKey), "periodKey");
			var response = await httpClient.PostAsync(NavigationManager.BaseUri + "upload/obligation", formData);
			if (response.IsSuccessStatusCode)
			{
				// handle successful response
				obligations.ElementAt(selectedIndex).attachment = true;
				attachments[selectedPeriodKey] = fileName;
			}
			else
			{
				// handle error response
			}
		}


		public async Task HandleUploadFile(InputFileChangeEventArgs args)
		{
			var files = args.GetMultipleFiles();
			List<string> acceptedFileTypes = new List<string>();
			if (files != null)
			{
				var file = files[0];
				await UploadFile(file);
			}
		}

		public IEnumerable<UserDetail> Filter(List<UserDetail> data)
		{
			string search = search_string.ToLower();
			if (data == null)
				return new List<UserDetail>();

			return data.FindAll(user =>
			{
				if (search.IsNullOrEmpty())
					return true;

				if (user.OwnerName != null && (user.OwnerName.ToLower().Contains(search)))
					return true;
				if (user.BusinessName != null && (user.BusinessName.ToLower().Contains(search)))
					return true;
				if (user.Manager != null && (user.Manager.ToLower().Contains(search)))
					return true;
				if (user.Partner != null && (user.Partner.ToLower().Contains(search)))
					return true;
				return false;
			}
			);
		}
		public async Task OpenObligation(Obligation obligation)
		{
			await DialogService.OpenAsync<EditObligation>("View Obligation", new Dictionary<string, object> { { "obligation", obligation} },
				new DialogOptions() { Width = "1500px" });
		}

		async Task Submit(MouseEventArgs args, string periodKey, string start, string end)
		{
			var confirmationResult = await this.DialogService.Confirm("How would you like to provide your VAT information?", "", new ConfirmOptions { OkButtonText = "EXCEL BRIDGING", CancelButtonText = "ACCOUNTING RECORDS" });
			if (confirmationResult == true) // excel bridging
			{
				NavigationManager.NavigateTo("/excel/" + periodKey + "/" + start + "/" + end);

			}
			else // accounting records
			{

			}
		}

		protected async System.Threading.Tasks.Task Button0Click(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
		{
			NavigationManager.NavigateTo("/Account/UserRestrictedCall");
		}

		protected void Search()
		{
			clients = Filter(all_clients);
			Console.WriteLine("search");
			Console.WriteLine(search_string);
			StateHasChanged();
		}

		class DataItem
		{
			public string Quarter { get; set; }
			public double Revenue { get; set; }
		}

		DataItem[] revenue = new DataItem[] {
			new DataItem
			{
				Quarter = "Q1",
				Revenue = 30000
				},
			new DataItem
			{
				Quarter = "Q2",
				Revenue = 40000
			},
			new DataItem
			{
				Quarter = "Q3",
				Revenue = 50000
			},
			new DataItem
			{
				Quarter = "Q4",
				Revenue = 80000
			},
		};

		public class Test
		{
			public int TaxYear { get; set; }
		}

		Test test = new Test()
		{

		};

		public class TaxYear
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		List<TaxYear> taxYears = new List<TaxYear>()
		{
		new TaxYear() { Id = 1, Name = "2023/24" },
		new TaxYear() { Id = 2, Name = "2022/23" }
		};
	}
}