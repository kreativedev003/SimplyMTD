using Newtonsoft.Json;
using Radzen.Blazor;
using Radzen;
using SimplyMTD.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SimplyMTD.Models.MTD;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components.Forms;
using ExcelDataReader;
using Microsoft.AspNetCore.Http.HttpResults;
using SendGrid.Helpers.Mail;
using static SimplyMTD.Pages.Setting;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.AspNetCore.OData.Query;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SendGrid;

namespace SimplyMTD.Pages
{
	public class SheetModel
	{
		public string VatString { get; set; } = "";
		public string CellAddr { get; set; } = "";
		public int SheetNumber { get; set; } = 0;
		public double Amount { get; set; } = 0;
	};

	public partial class SubmitVAT
	{
		[Inject]
		protected IJSRuntime JSRuntime { get; set; }

		[Inject]
		public NotificationService NotificationService { get; set; }

		[Inject]
		public VATService VATService { get; set; }

        [Inject]
        protected SecurityService Security { get; set; }

        protected SimplyMTD.Models.ApplicationUser user;

        [Parameter]
		public dynamic periodKey { get; set; }

		[Parameter]
		public dynamic start { get; set; }
		
		[Inject]
		public IConfiguration configuration { get; set; }

		[Inject]
		public SecurityService securityService { get; set; }

		[Parameter]
		public dynamic end { get; set; }

		private List<List<List<double>>> excelData = new List<List<List<double>>>();

		private List<SheetModel> sheetModel = new List<SheetModel>(10);

		private bool isAllow = false;

		protected SimplyMTD.Models.VATReturn vatReturn;

		RadzenUpload upload;

		private List<SheetData> sheetNameData = new List<SheetData>();

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

		protected override async Task OnInitializedAsync()
		{
			vatReturn = new SimplyMTD.Models.VATReturn();
            user = await Security.GetUserById($"{Security.User.Id}");
			sheetModel = new List<SheetModel>(10);
			for(var i=0; i<9; i++)
			{
				SheetModel newItem = new SheetModel();
				newItem.VatString = string.Format("{0,2:D}.    {1}", i+1, vat_string[i]);
				newItem.SheetNumber = 0;
				newItem.CellAddr = "B" + (i + 2).ToString();
				newItem.Amount = 0;
				sheetModel.Add(newItem);
			}
        }

		

		public SubmitVAT()
		{

		}
		void OnProgress(UploadProgressArgs args, string name)
		{
			if (args.Progress == 100)
			{
				foreach (var file in args.Files)
				{
				}
			}
		}

		private async Task HandleFileSelected(InputFileChangeEventArgs e)
		{
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
						if(double.TryParse(cellValue.ToString(), out value))
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
			Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");

			for (var i=0; i<9; i++)
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
					col = GetOrder(letters)-1;
					row = Convert.ToInt32(numbers)-1;
				}
				int worksheetNumber = sheetModel[i].SheetNumber;

				if (worksheetNumber >= 0 && excelData.Count > worksheetNumber && row >= 0 && col >= 0 && excelData[worksheetNumber].Count > row && excelData[worksheetNumber][row].Count > col) {
					sheetModel[i].Amount = excelData[worksheetNumber][row][col];
				} else
				{
					sheetModel[i].Amount = 0;
				}
			}
		}

		void OnComplete(UploadCompleteEventArgs args)
		{
			List<string> amounts = JsonConvert.DeserializeObject<List<string>>(args.RawResponse);
			this.vatReturn.periodKey = periodKey;
			this.vatReturn.vatDueSales = float.Parse(amounts.ElementAt(1));
			this.vatReturn.vatDueAcquisitions = float.Parse(amounts.ElementAt(2));
			this.vatReturn.totalVatDue = float.Parse(amounts.ElementAt(3));
			this.vatReturn.vatReclaimedCurrPeriod = float.Parse(amounts.ElementAt(4));
			this.vatReturn.netVatDue = float.Parse(amounts.ElementAt(5));
			this.vatReturn.totalValueSalesExVAT = int.Parse(amounts.ElementAt(6));
			this.vatReturn.totalValuePurchasesExVAT = int.Parse(amounts.ElementAt(7));
			this.vatReturn.totalValueGoodsSuppliedExVAT = int.Parse(amounts.ElementAt(8));
			this.vatReturn.totalAcquisitionsExVAT = int.Parse(amounts.ElementAt(9));
			this.vatReturn.finalised = true;
		}

		void OnChange(UploadChangeEventArgs args, string name)
		{
			foreach (var file in args.Files)
			{
			}

		}

		protected async Task SendEmail(VATReturn vatReturn)
		{
			string email = securityService.userDetail.Email;
			string sendGridApiKey = configuration.GetValue<string>("Sendgrid:API_KEY");
			if (string.IsNullOrEmpty(sendGridApiKey))
			{
				throw new Exception("The 'SendGridApiKey' is not configured");
			}

			var client = new SendGridClient(sendGridApiKey);
			var msg = new SendGridMessage()
			{
				From = new EmailAddress(configuration.GetValue<string>("Sendgrid:FROM_EMAIL"), "Vat Return is Submitted"),
				Subject = "Vat Return",
				PlainTextContent = string.Format("Agent Access Detail"),
				HtmlContent = string.Format($"periodKey: {vatReturn.periodKey}<br>" +
											$"vatDueSales: {vatReturn.vatDueSales}<br>" +
											$"vatDueAcquisitions: {vatReturn.vatDueAcquisitions}<br>" +
											$"totalVatDue: {vatReturn.totalVatDue}<br>" +
											$"vatReclaimedCurrPeriod: {vatReturn.vatReclaimedCurrPeriod}<br>" +
											$"netVatDue: {vatReturn.netVatDue}<br>" +
											$"totalValueSalesExVAT: {vatReturn.totalValueSalesExVAT}<br>" +
											$"totalValuePurchasesExVAT: {vatReturn.totalValuePurchasesExVAT}<br>" +
											$"totalValueGoodsSuppliedExVAT: {vatReturn.totalValueGoodsSuppliedExVAT}<br>" +
											$"totalAcquisitionsExVAT: {vatReturn.totalAcquisitionsExVAT}<br>")

			};
			msg.AddTo(new EmailAddress(email));
			var response = await client.SendEmailAsync(msg);
		}

		protected async Task SubmitVat()
		{
			this.vatReturn.periodKey = periodKey;
			this.vatReturn.vatDueSales = sheetModel[0].Amount;
			this.vatReturn.vatDueAcquisitions = sheetModel[1].Amount;
			this.vatReturn.totalVatDue = sheetModel[2].Amount;
			this.vatReturn.vatReclaimedCurrPeriod = sheetModel[3].Amount;
			this.vatReturn.netVatDue = sheetModel[4].Amount;
			this.vatReturn.totalValueSalesExVAT = (int)sheetModel[5].Amount;
			this.vatReturn.totalValuePurchasesExVAT = (int)sheetModel[6].Amount;
			this.vatReturn.totalValueGoodsSuppliedExVAT = (int)sheetModel[7].Amount;
			this.vatReturn.totalAcquisitionsExVAT = (int)sheetModel[8].Amount;
			this.vatReturn.finalised = true;

			object res = await VATService.submitVAT(vatReturn);

			await SendEmail(vatReturn);
			JObject resObject = JObject.FromObject(res);
			Console.Write(res.ToString());

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
		}

		void OnInvalidSubmit(FormInvalidSubmitEventArgs args)
		{
			//console.Log($"InvalidSubmit: {JsonSerializer.Serialize(args, new JsonSerializerOptions() { WriteIndented = true })}");
		}

		void Cancel()
		{
			//
		}

		void ShowNotification(NotificationMessage message)
		{
			NotificationService.Notify(message);

		}
	}
}