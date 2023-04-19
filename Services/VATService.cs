using Azure;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radzen;
using SimplyMTD.Data;
using SimplyMTD.Models;
using SimplyMTD.Models.MTD;
using SimplyMTD.Pages;
using System.Net.Http.Headers;

namespace SimplyMTD
{
    public partial class VATService
	{
        private readonly IConfiguration configuration;
		private readonly TokenProvider _store;
		private string token;
		public MTDContext _MTDContext;
		public SecurityService securityService;
		public NotificationService NotificationService { get; set; }

		public VATService(TokenProvider tokenProvider, 
			MTDContext context, 
			IConfiguration configuration,
			SecurityService securityService,
			NotificationService notificationService)
        {
            this._store = tokenProvider;
            this.configuration = configuration;
            this._MTDContext = context;
            this.token = _store.AccessToken;
			this.securityService = securityService;
			this.NotificationService = notificationService;
        }

        public async Task<List<UserDetail>> GetClientsForAgent()
        {
			List<UserDetail> userList = this._MTDContext.UserDetails.Where(user => user.AgentId == securityService.User.Id).ToList();
            return userList;
        }

        public async Task<bool> HmrcAsync()
        {
            if (this.token != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


		public void updateToken()
		{
			if (this.token.IsNullOrEmpty())
			{
				if (securityService.IsAuthenticated())
					this.token = securityService.userDetail.Token;
				else
					this.token = _store.AccessToken.ToString();
			}
		}

		public async Task<List<Obligation>> GetObligations(string vrn = "")
		{
			updateToken();
			var token = this.token; // Todo

			if(vrn.IsNullOrEmpty())
			{
				vrn = securityService.userDetail.Vrn;
			}

			Console.WriteLine("__________________________________________");
			Console.WriteLine(token);
			Console.WriteLine("\n");
			Console.WriteLine(vrn);

			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(configuration.GetValue<string>("Auth0:uri"));
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				HttpResponseMessage response = await client.GetAsync($"organisations/vat/{vrn}/obligations?from=2023-01-01&to=2023-12-04&status=O");
				String resp = await response.Content.ReadAsStringAsync();

				Console.WriteLine(resp);
				JObject jRes = JObject.FromObject(JsonConvert.DeserializeObject(resp));
				
				if (jRes["code"] != null)
				{
					ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, 
						Summary = "Obligation: " + jRes["code"].ToString(), Detail = jRes["message"].ToString(), Duration = 4000 });
				} else
				{
					ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Obligation", Detail = "Successfully loaded", Duration = 4000 });
				}

				ObligationBody obligationBody = JsonConvert.DeserializeObject<ObligationBody>(resp);
				Console.WriteLine(obligationBody);
				return obligationBody.obligations;
			}
		}

		public async Task<VATReturn> GetObligation(string periodKey, string vrn="")
		{
			updateToken();
			var token = this.token; // Todo
			if (vrn.IsNullOrEmpty())
			{
				vrn = securityService.userDetail.Vrn;
			}
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(configuration.GetValue<string>("Auth0:uri"));
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				Console.WriteLine($"organisations/vat/{vrn}/returns/" + periodKey);
				HttpResponseMessage response = await client.GetAsync($"organisations/vat/{vrn}/returns/" + periodKey);

				String resp = await response.Content.ReadAsStringAsync();
				VATReturn obligation = JsonConvert.DeserializeObject<VATReturn>(resp);
				return obligation;
			}
		}

		public async Task<List<Liability>> GetLiabilities(string vrn="")
		{
			updateToken();
			var token = this.token; // Todo
			if (vrn.IsNullOrEmpty())
			{
				vrn = securityService.userDetail.Vrn;
			}
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(configuration.GetValue<string>("Auth0:uri"));
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
				client.DefaultRequestHeaders.Add("Gov-Test-Scenario", "MULTIPLE_LIABILITIES");
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				HttpResponseMessage response = await client.GetAsync($"organisations/vat/{vrn}/liabilities?from=2023-01-01&to=2023-01-04");

				String resp = await response.Content.ReadAsStringAsync();

				JObject jRes = JObject.FromObject(JsonConvert.DeserializeObject(resp));

				if (jRes["code"] != null)
				{
					ShowNotification(new NotificationMessage
					{
						Severity = NotificationSeverity.Error,
						Summary = "Liability: " + jRes["code"].ToString(),
						Detail = jRes["message"].ToString(),
						Duration = 4000
					});
				}
				else
				{
					ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Liability", Detail = "Successfully loaded", Duration = 4000 });
				}


				LiabilityContainer liabilityContainer = JsonConvert.DeserializeObject<LiabilityContainer>(resp);
				return liabilityContainer.liabilities;
			}
		}

		public async Task<List<Payment>> GetPayments(string vrn="")
		{
			updateToken();
			var token = this.token; // Todo
			if (vrn.IsNullOrEmpty())
			{
				vrn = securityService.userDetail.Vrn;
			}
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(configuration.GetValue<string>("Auth0:uri"));
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
				client.DefaultRequestHeaders.Add("Gov-Test-Scenario", "MULTIPLE_PAYMENTS");
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				HttpResponseMessage response = await client.GetAsync($"organisations/vat/{vrn}/payments?from=2017-01-25&to=2017-06-25");

				String resp = await response.Content.ReadAsStringAsync();
				JObject jRes = JObject.FromObject(JsonConvert.DeserializeObject(resp));

				if (jRes["code"] != null)
				{
					ShowNotification(new NotificationMessage
					{
						Severity = NotificationSeverity.Error,
						Summary = "Payments: " + jRes["code"].ToString(),
						Detail = jRes["message"].ToString(),
						Duration = 4000
					});
				}
				else
				{
					ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Payments", Detail = "Successfully loaded", Duration = 4000 });
				}

				PaymentContainer paymentContainer = JsonConvert.DeserializeObject<PaymentContainer>(resp);
				return paymentContainer.payments;
			}
		}

		public async Task<object> submitVAT(VATReturn vatReturn)
		{
			updateToken();
			var token = this.token; // Todo
			var vrn = securityService.userDetail.Vrn;

			using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(configuration.GetValue<string>("Auth0:uri"));
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				vatReturn.finalised = true;

                var response = await client.PostAsJsonAsync($"organisations/vat/{vrn}/returns", vatReturn);

				string json = JsonConvert.SerializeObject(response);

				Console.WriteLine(token);
				Console.WriteLine(vrn);

				// Todo: customize
				var responseBody = await response.Content.ReadAsStringAsync();
				var responseJson = JsonConvert.DeserializeObject(responseBody);
				Console.WriteLine(responseBody);

				return responseJson;
            }
        }

		public async Task<object> submitVATByGuest(string vrn, VATReturn vatReturn, string email)
		{
			updateToken();
			var token = _store.AccessToken;
			Console.WriteLine("===========================================");
			Console.WriteLine(token.ToString());
			Console.WriteLine(vrn.ToString());

			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(configuration.GetValue<string>("Auth0:uri"));
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				vatReturn.finalised = true;

				var response = await client.PostAsJsonAsync($"organisations/vat/{vrn}/returns", vatReturn);

				string json = JsonConvert.SerializeObject(response);

				Console.WriteLine(token);
				Console.WriteLine(vrn);

				// Todo: customize
				var responseBody = await response.Content.ReadAsStringAsync();
				var responseJson = JsonConvert.DeserializeObject(responseBody);
				Console.WriteLine(responseBody);

				return responseJson;
			}
		}
		void ShowNotification(NotificationMessage message)
		{
			NotificationService.Notify(message);
		}

		public async Task<(string, string)> LookupVrn(string vrn)
		{
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(configuration.GetValue<string>("Auth0:uri"));
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));

				// organisations/vat/check-vat-number/lookup/{targetVrn}
				var response = await client.GetAsync($"organisations/vat/check-vat-number/lookup/{vrn}");
				var responseBody = await response.Content.ReadAsStringAsync();
				var responseJson = JsonConvert.DeserializeObject(responseBody);

				var jResponse = JObject.FromObject(responseJson);

				if (jResponse["target"] != null)
				{
					var businessName = jResponse["target"]["name"].ToString();
					var line1 = jResponse["target"]["address"]["line1"].ToString();
					var postcode = jResponse["target"]["address"]["postcode"].ToString();
					var countryCode = jResponse["target"]["address"]["countryCode"].ToString();
					var address = line1 + " (" + postcode + ") " + countryCode;

					return (businessName, address);
				}
				return ("", "");
			}
			/*
			try
			{
				UserDetail user = this._MTDContext.UserDetails.Where(user => user.Vrn == vrn).First();
				return (user.BusinessName, user.Address);
			} catch
			{
				return ("", "");
			}
			*/
		}
    }
}
