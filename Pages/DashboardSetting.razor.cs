using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Security.Claims;
using BlazorInputFile;
using Radzen.Blazor;
using Radzen;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ExcelDataReader;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;
using SimplyMTD.Models.MTD;
using SimplyMTD.Models;
using SendGrid.Helpers.Mail;
using SendGrid;
using Microsoft.JSInterop.Implementation;

namespace SimplyMTD.Pages
{

	public class SheetData
	{
		public int index {get; set;}
		public string data {get; set;}
		public SheetData(int index, string data)
		{
			this.index = index;
			this.data = data;
		}
	}

	public static class UriExtensions
	{
		public static string GetQueryParameter(this Uri uri, string key)
		{
			string[] queryParams = uri.Query.TrimStart('?').Split('&');

			foreach (string queryParam in queryParams)
			{
				string[] parts = queryParam.Split('=');

				if (parts.Length == 2 && parts[0].Equals(key, StringComparison.OrdinalIgnoreCase))
				{
					return Uri.UnescapeDataString(parts[1]);
				}
			}
			return "";
		}
	}

	public partial class DashboardSetting
	{
		[Inject]
		protected IJSRuntime JSRuntime { get; set; }

		[Inject]
		protected VATService VATService { get; set; }
		
		[Inject]
		protected SecurityService Security { get; set; }

		[Inject]
		protected NavigationManager Navigation { get; set; }
		[Inject]
		protected Blazored.SessionStorage.ISessionStorageService sessionStorage { get; set; }

		[Inject]
		protected TokenProvider TokenProvider { get; set; }
		[Inject]
		public NotificationService NotificationService { get; set; }

		[Inject]
		public IConfiguration configuration { get; set; }


		private List<List<List<double>>> excelData = new List<List<List<double>>>();
		private List<SheetData> sheetNameData = new List<SheetData>();

		private List<SheetModel> sheetModel = new List<SheetModel>(10);

		private bool isAllow = false;

		public string[] vat_string = new string[] {
			"VAT due in this period on sales and other outputs",
			"VAT due in the period on acquisitions of goods made in Northern Ireland from EU Member States",
			"Total VAT due (the sum of boxes 1 and 2)",
			"VAT reclaimed in the period on purchases and other inputs (incl. acquisitions in Northern Ireland from EU Member States)",
			"Net VAT to be paid to HMRC or reclaimed by you (Difference between boxes 3 and 4)",
			"Total value of sales and all other outputs excluding any VAT. Include your box 8 figure",
			"Total value of purchases and all other inputs excluding any VAT. Include your box 9 figure",
			"Total value of dispatches of goods and related costs (excluding VAT) from Northern Ireland to EU Member States",
			"Total value of acquisitions of goods and related costs (excluding VAT) made in Northern Ireland from EU Member States"
		};

		private int step = 1;

		private string vrn;
		private string business_name;
		private string reg_address;
		
		private string email;
		private bool isAuthenticated = false;
		private bool isAuthorised = false;
		private string period = "";
		private List<string> periodKeyData = new List<string>();
		private List<string> periodData = new List<string>();
		
		RadzenUpload upload;

		private string filename { get; set; } = "";
		private long filesize { get; set; } = 0;

		protected override async Task OnInitializedAsync()
		{
			var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
			var user = authState.User;
			isAuthenticated = user.Identity.IsAuthenticated;
			isAuthorised = !(TokenProvider.AccessToken.IsNullOrEmpty());

			//var Name = uri.GetQueryParameter("filename");
			//var Size = long.Parse(uri.GetQueryParameter("size"));
			//var ContentType = uri.GetQueryParameter("type");

			sheetModel = new List<SheetModel>(10);
			for (var i = 0; i < 9; i++)
			{
				SheetModel newItem = new SheetModel();
				newItem.VatString = string.Format("{0,2:D}.    {1}", i + 1, vat_string[i]);
				newItem.SheetNumber = 0;
				newItem.CellAddr = "B" + (i + 2).ToString();
				newItem.Amount = 0;
				sheetModel.Add(newItem);
			}

		}

		private async Task GetObligation()
		{
			var obligations = await VATService.GetObligations(vrn);
			if (obligations != null)
			{
				foreach (var obligation in obligations)
				{
					if (obligation.status == "O")
					{
						this.periodKeyData.Add(obligation.periodKey);
						this.periodData.Add(obligation.start);
						break;
					}
				}
			}

			Console.WriteLine(JsonConvert.SerializeObject(this.periodKeyData));
			Console.WriteLine(JsonConvert.SerializeObject(this.periodData));
		}

		private async Task HandleFileSelected(InputFileChangeEventArgs e)
		{
			Console.WriteLine("HandleFileSelected");

			List<string> sheetNames = new List<string>();
			using var ms = new MemoryStream();
			await e.File.OpenReadStream().CopyToAsync(ms);
			ms.Position = 0;
			using var reader = ExcelReaderFactory.CreateReader(ms);
			excelData.Clear();
			do
			{
				List<List<double>> worksheetData = new List<List<double>>();

				while (reader.Read())
				{
					List<double> newData = new List<double>();

					for (int i = 0; i < reader.FieldCount; i++)
					{
						var cellValue = reader.GetValue(i);
						double value;
						if (double.TryParse(cellValue.ToString(), out value))
						{
							newData.Add(value);
						}
						else
						{
							newData.Add(0);
						}

					}
					worksheetData.Add(newData);
				}
				excelData.Add(worksheetData);
				sheetNames.Add(reader.Name);
			} while (reader.NextResult());
			sheetNameData = sheetNames.Select((value, index) => new SheetData(index, value)).ToList<SheetData>();

			string content = string.Empty;
			string sheetnames = string.Empty;
			content = JsonConvert.SerializeObject(excelData);
			sheetnames = JsonConvert.SerializeObject(sheetNames);

			await sessionStorage.SetItemAsync<string>("filecontent", content);
			await sessionStorage.SetItemAsync<string>("sheetNames", sheetnames);

			UpdateSubmitData();
		}

		public static int GetOrder(string element)
		{
			int order = 0;
			for (int i = 0; i < element.Length; i++)
			{
				int digit = element[i] - 'A' + 1;
				order = order * 26 + digit;
			}
			return order;
		}


		void UpdateSubmitData()
		{
			for (var i = 0; i < 9; i++)
			{
				string cellAddr = sheetModel[i].CellAddr;
				string pattern = "([a-zA-Z]+)([0-9]+)";
				Match match = Regex.Match(cellAddr, pattern);

				int row = 0;
				int col = 0;

				if (match.Success)
				{
					string letters = match.Groups[1].Value;
					string numbers = match.Groups[2].Value;
					col = GetOrder(letters) - 1;
					row = Convert.ToInt32(numbers) - 1;
				}
				int worksheetNumber = sheetModel[i].SheetNumber;

				if (worksheetNumber >= 0 && excelData.Count > worksheetNumber && row >= 0 && col >= 0 && excelData[worksheetNumber].Count > row && excelData[worksheetNumber][row].Count > col)
				{
					sheetModel[i].Amount = excelData[worksheetNumber][row][col];
				}
				else
				{
					sheetModel[i].Amount = 0;
				}
			}
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				filename = await sessionStorage.GetItemAsync<string>("filename");
				filesize = await sessionStorage.GetItemAsync<long>("filesize");

				string filecontent = await sessionStorage.GetItemAsync<string>("filecontent");
				string sheetnames = await sessionStorage.GetItemAsync<string>("sheetNames");
				string state = await sessionStorage.GetItemAsync<string>("dashboard-setting");

				if (!filecontent.IsNullOrEmpty() && !isAuthenticated)
				{
					excelData = JsonConvert.DeserializeObject<List<List<List<double>>>>(filecontent);
					if (!filename.IsNullOrEmpty())
					{
						await JSRuntime.InvokeVoidAsync("setFileInput", filename);
					}
					if (!sheetnames.IsNullOrEmpty())
					{
						List<string> sheetData = JsonConvert.DeserializeObject<List<string>>(sheetnames);
						sheetNameData = sheetData.Select((value, index) => new SheetData(index, value)).ToList<SheetData>();
						Console.WriteLine(JsonConvert.SerializeObject(sheetData));
					}
				}

				if (state != null)
				{
					object oState = JsonConvert.DeserializeObject(state);
					JObject jState = JObject.FromObject(oState);
					if (jState["step"] != null)
					{
						step = int.Parse(jState["step"].ToString());
						vrn = jState["vrn"].ToString();
						business_name = jState["business"].ToString();
						reg_address = jState["address"].ToString();
						email = jState["email"].ToString();

						if (isAuthorised)
						{
							await GetObligation();
						}
					}
				}
				UpdateSubmitData();
				StateHasChanged();
			}
		}

		private async void LookupVRN()
		{
			string _business_name, _reg_address;
			(_business_name, _reg_address) = await VATService.LookupVrn(vrn);

			business_name = _business_name;
			reg_address = _reg_address;
			
			StateHasChanged();

		}

		private async void onAuthenticate()
		{
			string state = $"{{\"step\":{step}, \"vrn\":\"{vrn}\", \"business\":\"{business_name}\", \"address\":\"{reg_address}\", \"email\":\"{email}\"}}";
			await sessionStorage.SetItemAsync<string>("dashboard-setting", state);
		}

		private async void Submit()
		{
			if(isAuthenticated)
			{
				Navigation.NavigateTo("/dashboard");
			}

			var index = periodData.IndexOf(period);
			var periodKey = periodKeyData[index];

			Console.WriteLine("=============================");
			Console.WriteLine(periodKey);

			SimplyMTD.Models.VATReturn vatReturn = new Models.VATReturn();
			
			vatReturn.periodKey = periodKey;
			vatReturn.vatDueSales = sheetModel[0].Amount;
			vatReturn.vatDueAcquisitions = sheetModel[1].Amount;
			vatReturn.totalVatDue = sheetModel[2].Amount;
			vatReturn.vatReclaimedCurrPeriod = sheetModel[3].Amount;
			vatReturn.netVatDue = sheetModel[4].Amount;
			vatReturn.totalValueSalesExVAT = (int)sheetModel[5].Amount;
			vatReturn.totalValuePurchasesExVAT = (int)sheetModel[6].Amount;
			vatReturn.totalValueGoodsSuppliedExVAT = (int)sheetModel[7].Amount;
			vatReturn.totalAcquisitionsExVAT = (int)sheetModel[8].Amount;
			vatReturn.finalised = true;

			var res = await VATService.submitVATByGuest(vrn, vatReturn, email);

			JObject resObject = JObject.FromObject(res);

			if (resObject["code"] == null)
			{
				ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Successfully Submitted", Detail = "", Duration = 4000 });
			}
			else
			{
				string title = string.Empty;
				string content = string.Empty;

				if (resObject["errors"] == null)
				{
					title = resObject["code"].ToString();
					content = resObject["message"].ToString();
				}
				else
				{
					title = resObject["errors"][0]["code"].ToString();
					content = resObject["errors"][0]["message"].ToString();
				}
				ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = title, Detail = content, Duration = 4000 });
			}


			//email notification
			string sendGridApiKey = configuration.GetValue<string>("Sendgrid:API_KEY");
			if (string.IsNullOrEmpty(sendGridApiKey))
			{
				throw new Exception("The 'SendGridApiKey' is not configured");
			}

			var client = new SendGridClient(sendGridApiKey);
			var msg = new SendGridMessage()
			{
				From = new EmailAddress(configuration.GetValue<string>("Sendgrid:FROM_EMAIL"), "Submit Vat"),
				Subject = "Submit Vat",
				PlainTextContent = string.Format("Submit Vat as a guest"),
				HtmlContent = string.Format("Submit Vat as a guest")
			};
			msg.AddTo(new EmailAddress(email));
			var response = await client.SendEmailAsync(msg);
		}

		void ShowNotification(NotificationMessage message)
		{
			NotificationService.Notify(message);
		}
		private async void Exit()
		{
			Navigation.NavigateTo("/");
		}

		private void next_step()
		{
			bool flag = false;

			switch(step)
			{
				case 1:
					if (!business_name.IsNullOrEmpty() && !reg_address.IsNullOrEmpty())
						flag = true;
					break;
				case 2:
					if (isAuthorised && !period.IsNullOrEmpty())
						flag = true;
					break;
				case 3:
					if(isAllow == true)
						flag = true;
					break;
			}
			if(flag)
				step++;
		}

		private void prev_step()
		{
			step--;
		}
	}
}
