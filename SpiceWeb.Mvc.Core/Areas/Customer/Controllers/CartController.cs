using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceWeb.Mvc.Core.Data;
using SpiceWeb.Mvc.Core.Models;
using SpiceWeb.Mvc.Core.Models.ViewModels;
using SpiceWeb.Mvc.Core.Utility;
using Stripe;
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
        private readonly IEmailSender _emailSender;

        [BindProperty] //langsung binding data digunakan untuk menampung OrderDetailsCart agar tidak perlu menambhakan parameter input disetiap Create/Edit/Delete POST
        public OrderDetailsCart detailCart { get; set; }

        public CartController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPOST(string stripeToken)
        {
            //get login user id
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            detailCart.listCart = await _db.ShoppingCart.Where(x => x.ApplicationUserId == claim.Value).ToListAsync();

            detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            detailCart.OrderHeader.OrderDate = DateTime.Now;
            detailCart.OrderHeader.UserId = claim.Value;
            detailCart.OrderHeader.Status = SD.PaymentStatusPending;
            //detailCart.OrderHeader.PickupTime = Convert.ToDateTime(detailCart.OrderHeader.PickupDate.ToShortDateString() + " " + detailCart.OrderHeader.PickupTime.ToShortTimeString());
            detailCart.OrderHeader.PickupTime = detailCart.OrderHeader.PickupTime;

            List<OrderDetails> orderDetailList = new List<OrderDetails>();
            _db.OrderHeader.Add(detailCart.OrderHeader);
            await _db.SaveChangesAsync(); //save order header

            detailCart.OrderHeader.OrderTotalOriginal = 0;

            foreach (var item in detailCart.listCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(x => x.Id == item.MenuItemId);
                OrderDetails orderDetails = new OrderDetails()
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = detailCart.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                detailCart.OrderHeader.OrderTotalOriginal += orderDetails.Count * orderDetails.Price;
                _db.OrderDetails.Add(orderDetails);
            }

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
            else
            {
                detailCart.OrderHeader.OrderTotal = detailCart.OrderHeader.OrderTotalOriginal;
            }
            detailCart.OrderHeader.CouponCodeDiscount = detailCart.OrderHeader.OrderTotalOriginal - detailCart.OrderHeader.OrderTotal;

            _db.ShoppingCart.RemoveRange(detailCart.listCart); //remove shopping card after successfull insert into oder header and order detail
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0); //remove session shopping cart to be 0
            await _db.SaveChangesAsync();

            //--STRIPE START --//
            //stripe(online payment) request token => untuk melakukan request pada Stripe Server
            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(detailCart.OrderHeader.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID : " + detailCart.OrderHeader.Id,
                Source = stripeToken
            };
            var service = new ChargeService();
            Charge charge = service.Create(options); //melakukan request pada stripe server
            if (charge.BalanceTransactionId == null)
            {
                detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            else
            {
                detailCart.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status.ToLower() == "succeeded")
            {
                //send email menggunakan send grid -> karena belum mendaptkan key send grid maka tidak bisa mengirimkan email
                //await _emailSender.SendEmailAsync(_db.Users.Where(x => x.Id == claim.Value).FirstOrDefault().Email, "SPice - Order Created " + detailCart.OrderHeader.Id.ToString(), "Order Has been Submitted successfully");

                detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                detailCart.OrderHeader.Status = SD.StatusSubmitted;
            }
            else
            {
                detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            //--STRIPE END --//

            await _db.SaveChangesAsync();

            //return RedirectToAction("Index", "Home");

            //redirect langsung ke OrderController dan ke method Confirm
            return RedirectToAction("Confirm", "Order", new { Id = detailCart.OrderHeader.Id });
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
            if (cart.Count == 1)
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
