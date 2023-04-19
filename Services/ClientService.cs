using Newtonsoft.Json;
using SimplyMTD.Data;
using SimplyMTD.Models;
using SimplyMTD.Models.MTD;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SimplyMTD
{
    public partial class ClientService
	{
		private MTDContext context;
        private readonly IConfiguration configuration;
        private readonly TokenProvider _store;
        private readonly string token;
        private SecurityService securityService;

        public ClientService(TokenProvider tokenProvider, 
                            MTDContext context, 
                            IConfiguration configuration,
                            SecurityService securityService)
		{
            this.context = context;
            this._store = tokenProvider;
            this.configuration = configuration;
            this.token = _store.AccessToken;
            this.securityService = securityService;
        }

        public async Task CreateClient(UserDetail client)
        {
            if (client == null)
            {
                return;
            }
            UserDetail user = this.context.UserDetails.Where(user => user.Vrn == client.Vrn).First();
            if(user == null)
            {
                return;
            }
            user.AgentId = client.AgentId;
            user.ClientId = client.ClientId;
            user.BusinessName = client.BusinessName;
            user.BusinessType = client.BusinessType;
            user.Address = client.Address;
            user.Address2 = client.Address2;
            user.OwnerName = client.OwnerName;
            user.PostCode = client.PostCode;
            user.Email = client.Email;
            user.PhoneNumber = client.PhoneNumber;
            user.Partner = client.Partner;
            user.Manager = client.Manager;
            user.Subscription = client.Subscription;
            user.Authorisation = client.Authorisation;

            context.UserDetails.Update(user);
            this.context.SaveChanges();
        }

        public async Task DeleteClient(UserDetail client)
        {
            if (client == null)
            {
                return;
            }
            UserDetail user = this.context.UserDetails.Where(user => user.Vrn == client.Vrn).First();
            if (user == null)
            {
                return;
            }
            user.AgentId = "";
            this.context.UserDetails.Update(user);
            this.context.SaveChanges();
        }

        public async Task UpdateClient(UserDetail client)
        {
            if (client == null)
            {
                return;
            }
            UserDetail user = this.context.UserDetails.Where(user => user.Id == client.Id).First();
            if (user == null)
            {
                return;
            }

            user.ClientId = client.ClientId;
            user.BusinessName = client.BusinessName;
            user.BusinessType = client.BusinessType;
            user.Address = client.Address;
            user.Address2 = client.Address2;
            user.OwnerName = client.OwnerName;
            user.PostCode = client.PostCode;
            user.Email = client.Email;
            user.PhoneNumber = client.PhoneNumber;
            user.Partner = client.Partner;
            user.Manager = client.Manager;
            user.Subscription = client.Subscription;
            user.Authorisation = client.Authorisation;

            context.UserDetails.Update(user);
            this.context.SaveChanges();
        }

        public async Task<string> SendRequest(UserDetail user)
        {
            if (user == null) {
                return "User Detail is not correct!";
            }

            var token = this.token; // Todo

			using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(configuration.GetValue<string>("Auth0:uri"));
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var data = new {
                    service = new String[]{ "MTD-VAT" },
                    clientType = "business",
                    clientIdType = "vrn",
                    clientId = user.Vrn,
                    knownFact = user.RegDate.ToString("yyyy-MM-dd")
                };

                var payload = JsonConvert.SerializeObject(data);
                Console.WriteLine("SEND-REQUEST_SEND-REQUEST_SEND-REQUEST_SEND-REQUEST_SEND-REQUEST");
				Console.WriteLine(token);
				Console.WriteLine(payload);
                var arn = securityService.userDetail.Vrn;

                HttpResponseMessage response = await client.PostAsync($"agents/{arn}/invitations", new StringContent(payload, Encoding.UTF8, "application/json"));
                if(response.IsSuccessStatusCode)
                {
					if (response.Headers.TryGetValues("Location", out var locationHeaderValues))
					{
						var locationString = locationHeaderValues.FirstOrDefault();
						// Use the location string

						UserDetail _user = context.UserDetails.First(us => us.UserId == user.UserId);
                        _user.InvitationUrl = locationString;
                        _user.Authorisation = 1;
						context.UserDetails.Update(_user);
						context.SaveChanges();
					}
					return "";
				}
                else
                {
					String resp = await response.Content.ReadAsStringAsync();
					JObject result = JObject.Parse(resp);
					return result["message"].ToString();
				}
                return "";
            }
        }
		public async Task<string> CancelRequest(UserDetail user)
		{
			if (user == null)
			{
				return "User Detail is not correct!";
			}

			var token = this.token; // Todo

			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(configuration.GetValue<string>("Auth0:uri"));
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				HttpResponseMessage response = await client.DeleteAsync(user.InvitationUrl);
				if (response.IsSuccessStatusCode)
				{
					UserDetail _user = context.UserDetails.First(us => us.UserId == user.UserId);
					_user.InvitationUrl = "";
					_user.Authorisation = 0;
					context.UserDetails.Update(_user);
					context.SaveChanges();
					return "";
				}
				else
				{
					String resp = await response.Content.ReadAsStringAsync();
					JObject result = JObject.Parse(resp);
					return result["message"].ToString();
				}
			}
		}
		public async Task DeleteW8(W8 w8user)
		{
			if (w8user == null)
			{
				return;
			}
			W8 user = this.context.W8.Where(user => user.Id == w8user.Id).First();
			if (user == null)
			{
				return;
			}
			this.context.W8.Remove(user);
			this.context.SaveChanges();
		}

		public async Task UpdateW8(W8 w8user)
		{
			if (w8user == null)
			{
				return;
			}

			W8 user = this.context.W8.Where(user => user.Id == w8user.Id).First();
			if (user == null)
			{
				return;
			}

            user.Email = w8user.Email;
            user.Name = w8user.Name;

			context.W8.Update(user);
			this.context.SaveChanges();
		}

		public async Task CreateW8(W8 w8user)
        {
            if (w8user == null)
            {
                return;
            }

            w8user.Agent = securityService.userDetail.UserId;
            await context.W8.AddAsync(w8user);
			this.context.SaveChanges();
		}
	}
}
