using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyMTD.Models.MTD
{
    [Table("W8", Schema = "dbo")]
    public partial class W8
    {
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public string Id { get; set; }

		public string ClientId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Agent { get; set; }

        public string Password { get; set; }

        public int Permission { get; set; }

	}
}