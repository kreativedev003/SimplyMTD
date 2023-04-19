using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Quic;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
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
using Microsoft.JSInterop;
using Microsoft.OData;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;
using SimplyMTD.Models;
using static System.Net.WebRequestMethods;

namespace SimplyMTD.Pages
{
	public partial class EditObligation
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
		public MTDService _MTDService { get; set; }

		[Inject]
		HttpClient Http { get; set; }

		protected IEnumerable<Obligation> obligations;

		protected IEnumerable<Liability> liabilities;

		protected IEnumerable<Payment> payments;

		protected RadzenDataGrid<Obligation> grid1;

		protected RadzenDataGrid<Liability> grid2;

		protected RadzenDataGrid<Payment> grid3;

		protected bool isLoading = false;

		[Parameter]
		public Obligation obligation { get; set; }

		protected VATReturn vatReturn;

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
			isLoading = true;
			await Load();
			isLoading = false;
		}

		protected async System.Threading.Tasks.Task Load()
		{
			vatReturn = await VATService.GetObligation(obligation.periodKey);
		}

		protected async void Download()
		{
			List<List<string>> data = new List<List<string>>();
			string[] vat_amount = new string[]
			{
				vatReturn.vatDueSales.ToString(),
				vatReturn.vatDueAcquisitions.ToString(),
				vatReturn.totalVatDue.ToString(),
				vatReturn.vatReclaimedCurrPeriod.ToString(),
				vatReturn.netVatDue.ToString(),
				vatReturn.totalValueSalesExVAT.ToString(),
				vatReturn.totalValuePurchasesExVAT.ToString(),
				vatReturn.totalValueGoodsSuppliedExVAT.ToString(),
				vatReturn.totalAcquisitionsExVAT.ToString()
			};

			for(var i=0; i<9; i++)
			{
				List<string> row = new List<string>();
				row.Add((i + 1).ToString());
				row.Add(vat_string[i]);
				row.Add(vat_amount[i]);

				data.Add(row);
			}

			string csv = string.Join("\n", data.Select(row => string.Join(",", row)));
			await JSRuntime.InvokeAsync<object>("saveAsFile", $"vat_return_{obligation.start}~{obligation.end}.csv", csv);
		}

		protected async void Exit()
		{
			DialogService.Close();
		}
	}
}