using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpiceWeb.Mvc.Core.Data;
using SpiceWeb.Mvc.Core.Models;
using SpiceWeb.Mvc.Core.Models.ViewModels;
using SpiceWeb.Mvc.Core.Utility;

namespace SpiceWeb.Mvc.Core.Controllers
{
    //menambahkan area agar dapat diketahui bahwa controller Home merupakan area dari Customer, jika tidak ada maka akan Error
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            IndexViewModel indexVM = new IndexViewModel()
            {
                MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync(),
                Category = await _db.Category.ToListAsync(),
                Coupon = await _db.Coupon.Where(m => m.IsActive == true).ToListAsync()
            };

            return View(indexVM);
        }

        //GET - DETAIL
        [Authorize] //user must login to access this
        public async Task<IActionResult> Details(int Id)
        {
            var menuItemFromDb = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).Where(x => x.Id == Id).FirstOrDefaultAsync();

            ShoppingCart cartObj = new ShoppingCart()
            {
                MenuItem = menuItemFromDb,
                MenuItemId = menuItemFromDb.Id
            };

            //check session untk shopping cart
            var calimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = calimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim!=null)
            {
                var count = _db.ShoppingCart.Where(x => x.ApplicationUserId == claim.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count); //SD.ssShoppingCartCount adalah nama session
            }


            return View(cartObj);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Details(ShoppingCart CartObject)
        {
            CartObject.Id = 0;
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                CartObject.ApplicationUserId = claim.Value;

                ShoppingCart cartFromDb = await _db.ShoppingCart.Where(x => x.ApplicationUserId == CartObject.ApplicationUserId
                && x.MenuItemId == CartObject.MenuItemId).FirstOrDefaultAsync();

                if (cartFromDb == null)
                {
                    //jika data belum pernah ada
                    await _db.ShoppingCart.AddAsync(CartObject);
                }
                else
                {
                    //jika data sudah ada
                    cartFromDb.Count = cartFromDb.Count + cartFromDb.Count;
                }

                await _db.SaveChangesAsync();

                //menambahkan jumlah shopping cart menggunakan session
                var count = _db.ShoppingCart.Where(x => x.ApplicationUserId == CartObject.ApplicationUserId).ToList().Count();
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);

                return RedirectToAction(nameof(Index));
            }
            else
            {
                var menuItemFromDb = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).Where(x => x.Id == CartObject.MenuItemId).FirstOrDefaultAsync();

                ShoppingCart cartObj = new ShoppingCart()
                {
                    MenuItem = menuItemFromDb,
                    MenuItemId = menuItemFromDb.Id
                };

                return View(CartObject);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
