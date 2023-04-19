namespace SimplyMTD.Models
{
    public partial class VATReturn
    {
        public string periodKey { get; set; }

        public double vatDueSales { get; set; }

        public double vatDueAcquisitions { get; set; }

        public double totalVatDue { get; set; }

        public double vatReclaimedCurrPeriod { get; set; }

        public double netVatDue { get; set; }

        public int totalValueSalesExVAT { get; set; }

        public int totalValuePurchasesExVAT { get; set; }

        public int totalValueGoodsSuppliedExVAT { get; set; }

        public int totalAcquisitionsExVAT { get; set; }

        public bool finalised { get; set; }
    }
}
