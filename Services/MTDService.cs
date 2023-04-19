using System;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Radzen;

using SimplyMTD.Data;
using SimplyMTD.Models.MTD;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Identity;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.IdentityModel.Tokens;

namespace SimplyMTD
{
	public partial class MTDService
	{
		MTDContext Context
		{
			get
			{
				return this.context;
			}
		}

		private readonly MTDContext context;
		private readonly ApplicationIdentityDbContext identityContext;
		private readonly NavigationManager navigationManager;
		public int accountType = 1;

		public MTDService(	MTDContext context, 
							NavigationManager navigationManager,
							ApplicationIdentityDbContext identityContext)
		{
			this.context = context;
			this.identityContext = identityContext;
			this.navigationManager = navigationManager;
		}

		public void Reset() => Context.ChangeTracker.Entries().Where(e => e.Entity != null).ToList().ForEach(e => e.State = EntityState.Detached);


		public async Task ExportPlaningsToExcel(Query query = null, string fileName = null)
		{
			navigationManager.NavigateTo(query != null ? query.ToUrl($"export/mtd/planings/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/mtd/planings/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
		}

		public async Task ExportPlaningsToCSV(Query query = null, string fileName = null)
		{
			navigationManager.NavigateTo(query != null ? query.ToUrl($"export/mtd/planings/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/mtd/planings/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
		}

		partial void OnPlaningsRead(ref IQueryable<SimplyMTD.Models.MTD.Planing> items);

		public async Task<IQueryable<SimplyMTD.Models.MTD.Planing>> GetPlanings(Query query = null)
		{
			var items = Context.Planings.AsQueryable();

			if (query != null)
			{
				if (!string.IsNullOrEmpty(query.Expand))
				{
					var propertiesToExpand = query.Expand.Split(',');
					foreach (var p in propertiesToExpand)
					{
						items = items.Include(p.Trim());
					}
				}

				if (!string.IsNullOrEmpty(query.Filter))
				{
					if (query.FilterParameters != null)
					{
						items = items.Where(query.Filter, query.FilterParameters);
					}
					else
					{
						items = items.Where(query.Filter);
					}
				}

				if (!string.IsNullOrEmpty(query.OrderBy))
				{
					items = items.OrderBy(query.OrderBy);
				}

				if (query.Skip.HasValue)
				{
					items = items.Skip(query.Skip.Value);
				}

				if (query.Top.HasValue)
				{
					items = items.Take(query.Top.Value);
				}
			}

			OnPlaningsRead(ref items);

			return await Task.FromResult(items);
		}

		partial void OnPlaningGet(SimplyMTD.Models.MTD.Planing item);

		public async Task<SimplyMTD.Models.MTD.Planing> GetPlaningById(string id)
		{
			var items = Context.Planings
							  .AsNoTracking()
							  .Where(i => i.Id == id);


			var itemToReturn = items.FirstOrDefault();

			OnPlaningGet(itemToReturn);

			return await Task.FromResult(itemToReturn);
		}

		public async Task<string> GetAttachmentByPeriodKey(string key)
		{
			var items = Context.Attachment
							  .AsNoTracking()
							  .Where(i => i.PeriodKey == key);


			var itemToReturn = items.FirstOrDefault();
			string attach = (itemToReturn == null || itemToReturn.AttachDoc.IsNullOrEmpty()) ? "" : itemToReturn.AttachDoc;
			return await Task.FromResult(attach);
		}

		public async Task removeAttach(string key)
		{
			var item = Context.Attachment
							  .Where(i => i.PeriodKey == key)
							  .FirstOrDefault();
			if(item == null) return;
			item.AttachDoc = null;

			context.Update(item);
			await context.SaveChangesAsync();
		}

		public async Task setAttach(string key, string filename)
		{
			var item = Context.Attachment
							  .Where(i => i.PeriodKey == key)
							  .FirstOrDefault();
			if (item == null)
			{
				Attachment data = new Attachment
				{
					PeriodKey = key,
					AttachDoc = filename
				};
				context.Attachment.Add(data);
			}
			else
			{
				item.AttachDoc = filename;
				context.Attachment.Update(item);
			}
			await context.SaveChangesAsync();
		}

		partial void OnPlaningCreated(SimplyMTD.Models.MTD.Planing item);

		partial void OnAfterPlaningCreated(SimplyMTD.Models.MTD.Planing item);

		public async Task<SimplyMTD.Models.MTD.Planing> CreatePlaning(SimplyMTD.Models.MTD.Planing planing)
		{
			OnPlaningCreated(planing);

			var existingItem = Context.Planings
							  .Where(i => i.Id == planing.Id)
							  .FirstOrDefault();

			if (existingItem != null)
			{
				throw new Exception("Item already available");
			}

			try
			{
				Context.Planings.Add(planing);
				Context.SaveChanges();
			}
			catch
			{
				Context.Entry(planing).State = EntityState.Detached;
				throw;
			}

			OnAfterPlaningCreated(planing);

			return planing;
		}

		public async Task<SimplyMTD.Models.MTD.Planing> CancelPlaningChanges(SimplyMTD.Models.MTD.Planing item)
		{
			var entityToCancel = Context.Entry(item);
			if (entityToCancel.State == EntityState.Modified)
			{
				entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
				entityToCancel.State = EntityState.Unchanged;
			}

			return item;
		}

		partial void OnPlaningUpdated(SimplyMTD.Models.MTD.Planing item);
		partial void OnAfterPlaningUpdated(SimplyMTD.Models.MTD.Planing item);

		public async Task<SimplyMTD.Models.MTD.Planing> UpdatePlaning(string id, SimplyMTD.Models.MTD.Planing planing)
		{
			OnPlaningUpdated(planing);

			var itemToUpdate = Context.Planings
							  .Where(i => i.Id == planing.Id)
							  .FirstOrDefault();

			if (itemToUpdate == null)
			{
				throw new Exception("Item no longer available");
			}

			var entryToUpdate = Context.Entry(itemToUpdate);
			entryToUpdate.CurrentValues.SetValues(planing);
			entryToUpdate.State = EntityState.Modified;

			Context.SaveChanges();

			OnAfterPlaningUpdated(planing);

			return planing;
		}

		partial void OnPlaningDeleted(SimplyMTD.Models.MTD.Planing item);
		partial void OnAfterPlaningDeleted(SimplyMTD.Models.MTD.Planing item);

		public async Task<SimplyMTD.Models.MTD.Planing> DeletePlaning(string id)
		{
			var itemToDelete = Context.Planings
							  .Where(i => i.Id == id)
							  .FirstOrDefault();

			if (itemToDelete == null)
			{
				throw new Exception("Item no longer available");
			}

			OnPlaningDeleted(itemToDelete);


			Context.Planings.Remove(itemToDelete);

			try
			{
				Context.SaveChanges();
			}
			catch
			{
				Context.Entry(itemToDelete).State = EntityState.Unchanged;
				throw;
			}

			OnAfterPlaningDeleted(itemToDelete);

			return itemToDelete;
		}


		partial void OnBillingGet(SimplyMTD.Models.MTD.Billing item);

		public async Task<SimplyMTD.Models.MTD.Billing> GetBillingByUserId(string userId)
		{
			var items = Context.Billings
							  .AsNoTracking()
							  .Where(i => i.UserId == userId);


			var itemToReturn = items.FirstOrDefault();

			OnBillingGet(itemToReturn);

			return await Task.FromResult(itemToReturn);
		}


		partial void OnBillingCreated(SimplyMTD.Models.MTD.Billing item);

		partial void OnAfterBillingCreated(SimplyMTD.Models.MTD.Billing item);

		public async Task<SimplyMTD.Models.MTD.Billing> CreateBilling(SimplyMTD.Models.MTD.Billing billing)
		{
			OnBillingCreated(billing);

			var existingItem = Context.Billings
							  .Where(i => i.Id == billing.Id)
							  .FirstOrDefault();

			if (existingItem != null)
			{
				throw new Exception("Item already available");
			}

			try
			{
				Context.Billings.Add(billing);
				Context.SaveChanges();
			}
			catch
			{
				Context.Entry(billing).State = EntityState.Detached;
				throw;
			}

			OnAfterBillingCreated(billing);

			return billing;
		}

		partial void OnBillingUpdated(SimplyMTD.Models.MTD.Billing item);
		partial void OnAfterBillingUpdated(SimplyMTD.Models.MTD.Billing item);

		public async Task<SimplyMTD.Models.MTD.Billing> UpdateBilling(string id, SimplyMTD.Models.MTD.Billing billing)
		{
			OnBillingUpdated(billing);

			var itemToUpdate = Context.Billings
							  .Where(i => i.Id == billing.Id)
							  .FirstOrDefault();

			if (itemToUpdate == null)
			{
				throw new Exception("Item no longer available");
			}

			var entryToUpdate = Context.Entry(itemToUpdate);
			entryToUpdate.CurrentValues.SetValues(billing);
			entryToUpdate.State = EntityState.Modified;

			Context.SaveChanges();

			OnAfterBillingUpdated(billing);

			return billing;
		}


		partial void OnAccountingGet(SimplyMTD.Models.MTD.Accounting item);

		public async Task<SimplyMTD.Models.MTD.Accounting> GetAccountingByUserId(string userId)
		{
			var items = identityContext.Accountings
							  .AsNoTracking()
							  .Where(i => i.UserId == userId);


			var itemToReturn = items.FirstOrDefault();

			OnAccountingGet(itemToReturn);

			return await Task.FromResult(itemToReturn);
		}


		partial void OnAccountantCreated(SimplyMTD.Models.MTD.Accountant item);

		partial void OnAfterAccountantCreated(SimplyMTD.Models.MTD.Accountant item);

		public async Task<SimplyMTD.Models.MTD.Accountant> CreateAccountant(SimplyMTD.Models.MTD.Accountant accountant)
		{
			OnAccountantCreated(accountant);

			var existingItem = identityContext.Accountants
							  .Where(i => i.Id == accountant.Id)
							  .FirstOrDefault();

			if (existingItem != null)
			{
				throw new Exception("Item already available");
			}

			try
			{
                identityContext.Accountants.Add(accountant);
                identityContext.SaveChanges();
			}
			catch
			{
				Context.Entry(accountant).State = EntityState.Detached;
				throw;
			}

			OnAfterAccountantCreated(accountant);

			return accountant;
		}

		partial void OnAccountingCreated(SimplyMTD.Models.MTD.Accounting item);

		partial void OnAfterAccountingCreated(SimplyMTD.Models.MTD.Accounting item);

		public async Task<SimplyMTD.Models.MTD.Accounting> CreateAccounting(SimplyMTD.Models.MTD.Accounting accounting)
		{
			OnAccountingCreated(accounting);

			var existingItem = identityContext.Accountings
							  .Where(i => i.Id == accounting.Id)
							  .FirstOrDefault();

			if (existingItem != null)
			{
				throw new Exception("Item already available");
			}

			try
			{
                identityContext.Accountings.Add(accounting);
                identityContext.SaveChanges();
			}
			catch
			{
                identityContext.Entry(accounting).State = EntityState.Detached;
				throw;
			}

			OnAfterAccountingCreated(accounting);

			return accounting;
		}

		partial void OnAccountingUpdated(SimplyMTD.Models.MTD.Accounting item);
		partial void OnAfterAccountingUpdated(SimplyMTD.Models.MTD.Accounting item);

		public async Task<SimplyMTD.Models.MTD.Accounting> UpdateAccounting(string id, SimplyMTD.Models.MTD.Accounting accounting)
		{
			OnAccountingUpdated(accounting);

			var itemToUpdate = identityContext.Accountings
							  .Where(i => i.Id == accounting.Id)
							  .FirstOrDefault();

			if (itemToUpdate == null)
			{
				throw new Exception("Item no longer available");
			}

			var entryToUpdate = identityContext.Entry(itemToUpdate);
			entryToUpdate.CurrentValues.SetValues(accounting);
			entryToUpdate.State = EntityState.Modified;

            identityContext.SaveChanges();

			OnAfterAccountingUpdated(accounting);

			return accounting;
		}






		partial void OnAccountantGet(SimplyMTD.Models.MTD.Accountant item);

		public async Task<SimplyMTD.Models.MTD.Accountant> GetAccountantByUserId(string userId)
		{
			var items = identityContext.Accountants
							  .AsNoTracking()
							  .Where(i => i.UserId == userId);


			var itemToReturn = items.FirstOrDefault();

			OnAccountantGet(itemToReturn);

			return await Task.FromResult(itemToReturn);
		}

		partial void OnAccountantUpdated(SimplyMTD.Models.MTD.Accountant item);
		partial void OnAfterAccountingUpdated(SimplyMTD.Models.MTD.Accountant item);

		public async Task<SimplyMTD.Models.MTD.Accountant> UpdateAccountant(string id, SimplyMTD.Models.MTD.Accountant accountant)
		{
			OnAccountantUpdated(accountant);

			var itemToUpdate = identityContext.Accountants
							  .Where(i => i.Id == accountant.Id)
							  .FirstOrDefault();

			if (itemToUpdate == null)
			{
				throw new Exception("Item no longer available");
			}

			var entryToUpdate = identityContext.Entry(itemToUpdate);
			entryToUpdate.CurrentValues.SetValues(accountant);
			entryToUpdate.State = EntityState.Modified;

            identityContext.SaveChanges();

			OnAfterAccountingUpdated(accountant);

			return accountant;
		}

		

		public async Task ExportUserDetailsToExcel(Query query = null, string fileName = null)
		{
			navigationManager.NavigateTo(query != null ? query.ToUrl($"export/mtd/userdetails/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/mtd/userdetails/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
		}

		public async Task ExportUserDetailsToCSV(Query query = null, string fileName = null)
		{
			navigationManager.NavigateTo(query != null ? query.ToUrl($"export/mtd/userdetails/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/mtd/userdetails/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
		}

		partial void OnUserDetailsRead(ref IQueryable<SimplyMTD.Models.MTD.UserDetail> items);

		public async Task<IQueryable<SimplyMTD.Models.MTD.UserDetail>> GetUserDetails(Query query = null)
		{

			/*var items = Context.UserDetails
				.Include(i => i.User.Roles.Where(x => x.Name == "Agent"))
					 //.Include(i => i.Role)
				.AsQueryable();*/
			var items = Context.UserDetails
				.Include(d => d.User)
				.ThenInclude(u => u.Roles)
				//.ThenInclude(u => u.Roles.Where(r => r.Name == "Agent"))
				
				.AsQueryable();
			

			if (query != null)
			{
				if (!string.IsNullOrEmpty(query.Expand))
				{
					var propertiesToExpand = query.Expand.Split(',');
					foreach (var p in propertiesToExpand)
					{
						items = items.Include(p.Trim());
					}
				}

				if (!string.IsNullOrEmpty(query.Filter))
				{
					if (query.FilterParameters != null)
					{
						items = items.Where(query.Filter, query.FilterParameters);
					}
					else
					{
						items = items.Where(query.Filter);
					}
				}

				if (!string.IsNullOrEmpty(query.OrderBy))
				{
					items = items.OrderBy(query.OrderBy);
				}

				if (query.Skip.HasValue)
				{
					items = items.Skip(query.Skip.Value);
				}

				if (query.Top.HasValue)
				{
					items = items.Take(query.Top.Value);
				}
			}

			OnUserDetailsRead(ref items);

			return await Task.FromResult(items);
		}



		partial void OnUserDetailGet(SimplyMTD.Models.MTD.UserDetail item);

		public async Task<SimplyMTD.Models.MTD.UserDetail> GetUserDetailById(string id)
		{
			var items = Context.UserDetails
							  .AsNoTracking()
							  .Where(i => i.Id == id);


			var itemToReturn = items.FirstOrDefault();

			OnUserDetailGet(itemToReturn);

			return await Task.FromResult(itemToReturn);
		}

		public async Task<SimplyMTD.Models.MTD.UserDetail> GetUserDetailByEmail(string email)
		{
			var items = Context.UserDetails
							  .AsNoTracking()
							  .Where(i => i.Email == email);


			var itemToReturn = items.FirstOrDefault();

			OnUserDetailGet(itemToReturn);

			return await Task.FromResult(itemToReturn);
		}

		public async Task<List<SimplyMTD.Models.MTD.W8>> GetW8UsersByUserId(string id)
		{
			var items = Context.W8.Where(w8 => w8.Agent == id).ToList();
			return await Task.FromResult(items);
		}

		public async Task<SimplyMTD.Models.MTD.UserDetail> GetUserDetailByUserId(string userId)
		{
			var items = Context.UserDetails
							  .AsNoTracking()
							  .Where(i => i.UserId == userId);


			var itemToReturn = items.FirstOrDefault();

			OnUserDetailGet(itemToReturn);

			return await Task.FromResult(itemToReturn);
		}

		partial void OnUserDetailCreated(SimplyMTD.Models.MTD.UserDetail item);
		partial void OnAfterUserDetailCreated(SimplyMTD.Models.MTD.UserDetail item);

		public async Task<SimplyMTD.Models.MTD.UserDetail> CreateUserDetail(SimplyMTD.Models.MTD.UserDetail userdetail)
		{
			OnUserDetailCreated(userdetail);

			var existingItem = Context.UserDetails
							  .Where(i => i.Id == userdetail.Id)
							  .FirstOrDefault();

			if (existingItem != null)
			{
				throw new Exception("Item already available");
			}

			try
			{
				Context.UserDetails.Add(userdetail);
				Context.SaveChanges();
			}
			catch
			{
				Context.Entry(userdetail).State = EntityState.Detached;
				throw;
			}

			OnAfterUserDetailCreated(userdetail);

			return userdetail;
		}

		public async Task<SimplyMTD.Models.MTD.UserDetail> CancelUserDetailChanges(SimplyMTD.Models.MTD.UserDetail item)
		{
			var entityToCancel = Context.Entry(item);
			if (entityToCancel.State == EntityState.Modified)
			{
				entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
				entityToCancel.State = EntityState.Unchanged;
			}

			return item;
		}

		partial void OnUserDetailUpdated(SimplyMTD.Models.MTD.UserDetail item);
		partial void OnAfterUserDetailUpdated(SimplyMTD.Models.MTD.UserDetail item);

		public async Task<bool> UpdateUserDetail(string id, SimplyMTD.Models.MTD.UserDetail userdetail)
		{
			OnUserDetailUpdated(userdetail);

			var itemToUpdate = Context.UserDetails
							  .Where(i => i.Id == userdetail.Id)
							  .FirstOrDefault();

			if (itemToUpdate == null)
			{
				throw new Exception("Item no longer available");
			}

			var entryToUpdate = Context.Entry(itemToUpdate);
			entryToUpdate.CurrentValues.SetValues(userdetail);
			entryToUpdate.State = EntityState.Modified;

			Context.SaveChanges();

			OnAfterUserDetailUpdated(userdetail);

			// return userdetail;
			return true;
		}

		partial void OnUserDetailDeleted(SimplyMTD.Models.MTD.UserDetail item);
		partial void OnAfterUserDetailDeleted(SimplyMTD.Models.MTD.UserDetail item);

		public async Task<SimplyMTD.Models.MTD.UserDetail> DeleteUserDetail(string id)
		{
			var itemToDelete = Context.UserDetails
							  .Where(i => i.Id == id)
							  .FirstOrDefault();

			if (itemToDelete == null)
			{
				throw new Exception("Item no longer available");
			}

			OnUserDetailDeleted(itemToDelete);


			Context.UserDetails.Remove(itemToDelete);

			try
			{
				Context.SaveChanges();
			}
			catch
			{
				Context.Entry(itemToDelete).State = EntityState.Unchanged;
				throw;
			}

			OnAfterUserDetailDeleted(itemToDelete);

			return itemToDelete;
		}


		partial void OnUserGet(SimplyMTD.Models.ApplicationUser item);

		public async Task<SimplyMTD.Models.ApplicationUser> GetUserById(string id)
		{
			var items = identityContext.AspNetUsers
							  .AsNoTracking()
							  .Where(i => i.Id == id).Include(i => i.UserDetail);


			var itemToReturn = items.FirstOrDefault();

			OnUserGet(itemToReturn);

			return await Task.FromResult(itemToReturn);
		}

	}
}