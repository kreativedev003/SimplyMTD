using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SimplyMTD.Models;
using SendGrid.Helpers.Mail;
using SendGrid;
using System.Net.Http.Headers;
using SimplyMTD.Models.MTD;
using Microsoft.AspNetCore.Components.Authorization;
using SimplyMTD.Data;
using Microsoft.EntityFrameworkCore;

namespace SimplyMTD.Controllers
{
    [Route("Client/[action]")]
    public partial class ClientController : ControllerBase
    {
        private readonly MTDContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly TokenProvider tokenProvider;
		// protected SimplyMTD.Models.MTD.UserDetail userDetail;

		// public MTDService MTDService { get; set; }

		public ClientController(MTDContext mtdContext, 
                                UserManager<ApplicationUser> userManager,
                                TokenProvider tokenProvider)
        {
            this.context = mtdContext;
            this.userManager = userManager;
            this.tokenProvider = tokenProvider;
		}

        public async Task<IActionResult> UserRestrictedCall(string key, string vrn)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userDetail = context.UserDetails.First(x => x.UserId == key);
                userDetail.Vrn = vrn;
                context.Update(userDetail);
                context.SaveChanges();
            }

            return Challenge(new AuthenticationProperties() { RedirectUri = "/Client/FromHMRC" }, "HMRC");

            string accessToken = await HttpContext.GetTokenAsync(IdentityConstants.ExternalScheme, "access_token");
            tokenProvider.AccessToken = accessToken;

            if (accessToken != null)
            {
                return Redirect($"~/dashboard");
               
            }
            else
            {
                return Challenge(new AuthenticationProperties() { RedirectUri = "/Client/FromHMRC" }, "HMRC");
            }
        }

        public async Task<IActionResult> FromHMRC()
        {
            string accessToken = await HttpContext.GetTokenAsync(IdentityConstants.ExternalScheme, "access_token");

            if (User.Identity.IsAuthenticated)
            {
                var user = await userManager.GetUserAsync(User);
                var userDetail = context.UserDetails.First(x => x.UserId == user.Id);
                userDetail.Token = accessToken;
                context.Update(userDetail);
                context.SaveChanges();
            }

            return Redirect($"~/dashboard");

		}

		public async Task<IActionResult> List()
		{
			string accessToken = await HttpContext.GetTokenAsync(IdentityConstants.ExternalScheme, "access_token");
			List<UserDetail> UserList = new List<UserDetail>();

			if (User.Identity.IsAuthenticated)
			{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                UserList = context.UserDetails.Where<UserDetail>(user => user.AgentId == userId).ToList();
                return Ok(UserList);
			}

			return Redirect($"~/dashboard");
		}

        public async Task<IActionResult> Detail(string id)
        {
            string accessToken = await HttpContext.GetTokenAsync(IdentityConstants.ExternalScheme, "access_token");
 
            if (User.Identity.IsAuthenticated)
            {
                var userList = context.UserDetails.Where(user => user.ClientId == id).ToList();
                if(userList.Count == 0) {
                    return BadRequest();
                }
                return Ok(userList.First<UserDetail>());
            }

            return Redirect($"~/hmrc-success");
        }
    }
}