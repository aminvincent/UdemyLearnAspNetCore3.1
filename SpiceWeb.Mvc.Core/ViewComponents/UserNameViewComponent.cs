using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceWeb.Mvc.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SpiceWeb.Mvc.Core.ViewComponents
{
    /*agar asp.net core mengetahui view component nama class harus menggunakan ViewComponent
    di akahir atau menggunakan inheritance dari ViewComponent
    */
    public class UserNameViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public UserNameViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var userFromDb = await _db.ApplicationUser.FirstOrDefaultAsync(x => x.Id == claims.Value);

            return View(userFromDb);
        }
    }
}
