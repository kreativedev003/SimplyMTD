using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyMTD.Models.MTD
{
    [Table("Attachment", Schema = "dbo")]
    public partial class Attachment
    {
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public string Id { get; set; }

		public string PeriodKey { get; set; }

		public string AttachDoc { get; set; }
	}
}