using DocumentFormat.OpenXml.Wordprocessing;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using SendGrid.Helpers.Mail;
using SendGrid;
using SimplyMTD.Models.MTD;

namespace SimplyMTD.Pages
{
    public partial class InformationRequest
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

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
        protected IConfiguration configuration { get; set; }

        [Parameter] public SimplyMTD.Models.MTD.UserDetail userDetail { get; set; } = new SimplyMTD.Models.MTD.UserDetail();

        protected IEnumerable<SimplyMTD.Models.ApplicationRole> roles;
        protected SimplyMTD.Models.ApplicationUser user;
        protected IEnumerable<string> userRoles;
        protected List<string> managers;
        protected List<string> partners;
        protected string error;
        protected bool errorVisible;
        protected DateTime date;
        protected string message = "";
        protected int option = 1;
        protected string[] tasks = { "Vat", "Tax" };

        [Inject]
        protected SecurityService Security { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
        protected override async Task OnInitializedAsync()
        {
            // user = new SimplyMTD.Models.ApplicationUser();

            roles = await Security.GetRoles();

            date = DateTime.Now;
            StateHasChanged();
        }

        protected async Task OnEmail()
        {
            string content = "";

            switch(option)
            {
                case 1:
                    content = "Request MTD Authorisation";
                    break;
                case 2:
                    content = "Request Information for Next Task";
                    break;
                case 3:
                    content = "Software Subscription Overdue Reminder";
                    break;
                case 4:
                    content = "Custom Email";
                    break;
            }

            string sendGridApiKey = configuration.GetValue<string>("Sendgrid:API_KEY");
            if (string.IsNullOrEmpty(sendGridApiKey))
            {
                throw new Exception("The 'SendGridApiKey' is not configured");
            }

            var client = new SendGridClient(sendGridApiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(configuration.GetValue<string>("Sendgrid:FROM_EMAIL"), "Information Request"),
                Subject = "Information Request",
                PlainTextContent = string.Format("Please confirm your Information Request"),
                HtmlContent = string.Format(content)
            };
            msg.AddTo(new EmailAddress(userDetail.Email));

            var response = await client.SendEmailAsync(msg);
        }

        protected async Task FormSubmit(SimplyMTD.Models.ApplicationUser user)
        {
            try
            {
                user.Roles = roles.Where(role => userRoles.Contains(role.Id)).ToList();
                await Security.CreateUser(user);
                DialogService.Close(null);
            }
            catch (Exception ex)
            {
                errorVisible = true;
                error = ex.Message;
            }
        }

        protected async Task CancelClick()
        {
            DialogService.Close(null);
        }
    }
}