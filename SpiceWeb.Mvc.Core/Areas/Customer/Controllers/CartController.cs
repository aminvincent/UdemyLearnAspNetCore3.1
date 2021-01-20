using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceWeb.Mvc.Core.Data;
using SpiceWeb.Mvc.Core.Models;
using SpiceWeb.Mvc.Core.Models.ViewModels;
using SpiceWeb.Mvc.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SpiceWeb.Mvc.Core.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        [BindProperty] //langsung binding data digunakan untuk menampung OrderDetailsCart agar tidak perlu menambhakan parameter input disetiap Create/Edit/Delete POST
        public OrderDetailsCart detailCart { get; set; }

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            detailCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };

            detailCart.OrderHeader.OrderTotal = 0;

            //get login user id
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _db.ShoppingCart.Where(x => x.ApplicationUserId == claim.Value);
            if (cart != null)
            {
                detailCart.listCart = cart.ToList();
            }

            foreach (var item in detailCart.listCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(x => x.Id == item.MenuItemId);
                detailCart.OrderHeader.OrderTotal = detailCart.OrderHeader.OrderTotal + (item.MenuItem.Price * item.Count);
                item.MenuItem.Description = SD.ConvertToRawHtml(item.MenuItem.Description); //convert raw html to menu item
                if (item.MenuItem.Description.Length > 100)
                {
                    item.MenuItem.Description = item.MenuItem.Description.Substring(0, 99) + "..."; //hanya menampilkan description 100 karakter
                }
            }
            detailCart.OrderHeader.OrderTotalOriginal = detailCart.OrderHeader.OrderTotal;

            //check session coupon code
            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                //get coupon code dari session
                detailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                //check coupon code pada db
                var couponFromDb = await _db.Coupon.Where(x => x.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                //implement coupon code pada OrderTotal
                detailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailCart.OrderHeader.OrderTotalOriginal);
            }

            return View(detailCart);
        }

        public async Task<IActionResult> Summary()
        {
            detailCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };

            detailCart.OrderHeader.OrderTotal = 0;

            //get login user id
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ApplicationUser applicationUser = await _db.ApplicationUser.Where(x => x.Id == claim.Value).FirstOrDefaultAsync();

            var cart = _db.ShoppingCart.Where(x => x.ApplicationUserId == claim.Value);
            if (cart != null)
            {
                detailCart.listCart = cart.ToList();
            }

            foreach (var item in detailCart.listCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(x => x.Id == item.MenuItemId);
                detailCart.OrderHeader.OrderTotal = detailCart.OrderHeader.OrderTotal + (item.MenuItem.Price * item.Count);
            }
            detailCart.OrderHeader.OrderTotalOriginal = detailCart.OrderHeader.OrderTotal;
            detailCart.OrderHeader.PickupName = applicationUser.Name;
            detailCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            detailCart.OrderHeader.PickupTime = DateTime.Now;

            //check session coupon code
            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                //get coupon code dari session
                detailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                //check coupon code pada db
                var couponFromDb = await _db.Coupon.Where(x => x.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                //implement coupon code pada OrderTotal
                detailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailCart.OrderHeader.OrderTotalOriginal);
            }

            return View(detailCart);
        }

        public IActionResult AddCoupon()
        {
            if (detailCart.OrderHeader.CouponCode == null)
            {
                detailCart.OrderHeader.CouponCode = "";
            }

            //simpan coupon code di session
            HttpContext.Session.SetString(SD.ssCouponCode, detailCart.OrderHeader.CouponCode);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            //reset coupon code session with empty value
            HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(x => x.Id == cartId);
            cart.Count += 1;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(x => x.Id == cartId);
            if (cart.Count==1)
            {
                //ketika count nya 1 maka hapus dari database shoppint cart karena ini fungsi Minus
                _db.ShoppingCart.Remove(cart);
                await _db.SaveChangesAsync();

                //hapus session karena di remove
                var cnt = _db.ShoppingCart.Where(x => x.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(x => x.Id == cartId);

            //ketika count nya 1 maka hapus dari database shoppint cart karena ini fungsi Minus
            _db.ShoppingCart.Remove(cart);
            await _db.SaveChangesAsync();

            //hapus session karena di remove
            var cnt = _db.ShoppingCart.Where(x => x.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);

            return RedirectToAction(nameof(Index));
        }
    }
}
