namespace SimplyMTD.Models
{
	public partial class Client
	{
		public string Id { get; set; }

		public string ClientName { get; set;}

		public string VatNumber { get; set; }

		public string NextVatPeriod { get; set; }

		public string DeadLine { get; set; }

		public string Manager { get; set; }

		public string Partner { get; set; }

		public int Authorisation { get; set; }

		public int Subscription { get; set; }

		public string Note { get; set; }

		public string Email { get; set; }
	}

	public partial class ClientContainer
	{
		public List<Client> clients { get; set; }
	}
}
