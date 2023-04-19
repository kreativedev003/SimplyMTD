using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using ExcelDataReader;

namespace SimplyMTD.Pages
{
	public partial class Home
	{
		[Inject]
		protected IJSRuntime JSRuntime { get; set; }

		[Inject]
		protected VATService VATService { get; set; }
		
		[Inject]
		protected SecurityService Security { get; set; }

		[Inject]
		protected NavigationManager NavigationManager { get; set; }

		[Inject]
		protected Blazored.SessionStorage.ISessionStorageService sessionStorage { get; set; }

		[Inject]
		protected TokenProvider TokenProvider { get; set; }

		private string HoverClass = string.Empty;
		private string SelectFile = string.Empty;
		protected override async Task OnInitializedAsync()
		{
			var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
			var user = authState.User;
			if(user.Identity.IsAuthenticated)
			{
				NavigationManager.NavigateTo("/dashboard");
			}

		}

		private void HandleDragEnter()
		{
			HoverClass = "dropzone-drag";
		}

		private void HandleDragLeave()
		{
			HoverClass = "";
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await sessionStorage.RemoveItemsAsync(new[] { "filename", "filesize", "filetype", "filecontent", "dashboard-setting"});
			}
		}
			private async Task HandleFileInputChange(InputFileChangeEventArgs e)
		{
			Console.WriteLine("InputChange");

			HoverClass = "";
			var files = e.GetMultipleFiles();
			List<string> acceptedFileTypes = new List<string>() { "image/png", "image/jpeg", "image/gif" };
			if (files != null)
			{
				var file = files[0];

				await sessionStorage.SetItemAsync("filename", file.Name);
				await sessionStorage.SetItemAsync("filesize", file.Size);
				await sessionStorage.SetItemAsync("filetype", file.ContentType);

				using var ms = new MemoryStream();
				await e.File.OpenReadStream().CopyToAsync(ms);
				ms.Position = 0;
				using var reader = ExcelReaderFactory.CreateReader(ms);
				List<List<List<double>>> excelData = new List<List<List<double>>>();
				List<string> sheetName = new List<string>();

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
					sheetName.Add(reader.Name);
				} while (reader.NextResult());

				await sessionStorage.SetItemAsync<string>("filecontent", JsonConvert.SerializeObject(excelData));
				await sessionStorage.SetItemAsync<string>("sheetNames", JsonConvert.SerializeObject(sheetName));
				TokenProvider.AccessToken = "";

				NavigationManager.NavigateTo("/dashboard-setting");
			}
		}

		private async Task HandleDropFile()
		{
			Console.WriteLine("DropFile");
			HoverClass = "";
		}


	}
}