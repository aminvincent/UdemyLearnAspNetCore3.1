using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceWeb.Mvc.Core.Data;
using SpiceWeb.Mvc.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceWeb.Mvc.Core.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _db.Coupon.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream()) //start reading a file 
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }
                    coupon.Picture = p1;
                }
                _db.Coupon.Add(coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); //redirect to index while success
            }
            return View(coupon); //while model state is not valid display as view 
        }

        //GET - EDIT
        public async Task<IActionResult> Edit(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            var coupon = await _db.Coupon.FindAsync(Id);
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        //POST - EDIT
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(Coupon coupon)
        {
            if (coupon.Id == 0)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                Coupon couponFromDb = await _db.Coupon.FindAsync(coupon.Id);

                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream()) //start reading a file 
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }
                    couponFromDb.Picture = p1;
                }

                couponFromDb.Name = coupon.Name;
                couponFromDb.Discount = coupon.Discount;
                couponFromDb.CouponType = coupon.CouponType;
                couponFromDb.MinimumAmount = coupon.MinimumAmount;
                couponFromDb.IsActive = coupon.IsActive;

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); //redirect to index while success
            }
            return View(coupon); //while model state is not valid display as view 
        }

        //GET - DETAIL
        public async Task<IActionResult> Detail(int? Id)
        {
            if (Id == null) return NotFound();

            var coupon = await _db.Coupon.FindAsync(Id);
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        //GET - DELETE
        public async Task<IActionResult> Delete(int? Id)
        {
            if (Id == null) return NotFound();

            var coupon = await _db.Coupon.FindAsync(Id);
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        //POST - DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? Id)
        {
            if (Id == null) return NotFound();

            var coupon = await _db.Coupon.FindAsync(Id);
            if (coupon == null)
            {
                return NotFound();
            }

            _db.Coupon.Remove(coupon);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
