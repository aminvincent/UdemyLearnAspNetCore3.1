using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceWeb.Mvc.Core.Data;
using SpiceWeb.Mvc.Core.Models;
using SpiceWeb.Mvc.Core.Utility;

namespace SpiceWeb.Mvc.Core.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment; //ini digunakan untuk mengambil/simpan gambar di directory web

        //langsung binding data digunakan untuk menampung menu item agar tidak perlu menambhakan parameter input disetiap Create/Edit/Delete POST
        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }

        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            MenuItemVM = new MenuItemViewModel()
            {
                Category = _db.Category,
                MenuItem = new Models.MenuItem() //untuk sub category tidak dtambahkan karene sub category akan menampilkan sesuai dengan category
            };
        }
        public async Task<IActionResult> Index()
        {
            var menuItems = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).ToListAsync();
            return View(menuItems);
        }

        //GET - CREATE
        public IActionResult Create()
        {
            return View(MenuItemVM); //langsung memanggil bind property yang sudah diberikan value pada constructor
        }

        //POST - CREATE
        [HttpPost, ActionName("Create")] //agar d view bisa d panggil dengan method default nya Create walaupun nama controllernya berbeda.
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePOST()
        {
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                return View(MenuItemVM);
            }

            _db.MenuItem.Add(MenuItemVM.MenuItem);
            await _db.SaveChangesAsync();

            //untuk upload simpan gambar pada folder Images yang ada di wwwroot
            string webrootpath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFormDb = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);
            if (files.Count > 0)
            {
                //file sudah pernah di upload
                var uploads = Path.Combine(webrootpath, "images"); //ambil dari folder images
                var extension = Path.GetExtension(files[0].FileName);

                using (var filesStream = new FileStream(Path.Combine(uploads, MenuItemVM.MenuItem.Id + extension), FileMode.Create))
                {
                    files[0].CopyTo(filesStream);
                }

                menuItemFormDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + extension;
            }
            else
            {
                //file belum pernah di upload
                var uploads = Path.Combine(webrootpath, @"images\" + SD.DefaultFoodImage);
                System.IO.File.Copy(uploads, webrootpath + @"\images\" + MenuItemVM.MenuItem.Id + ".png");
                menuItemFormDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + ".png";
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        //GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).FirstOrDefaultAsync(x => x.Id == id);
            MenuItemVM.SubCategory = await _db.SubCategory.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM); //langsung memanggil bind property yang sudah diberikan value pada constructor
        }

        //POST - EDIT
        [HttpPost, ActionName("Edit")] //agar d view bisa d panggil dengan method default nya Create walaupun nama controllernya berbeda.
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                MenuItemVM.SubCategory = await _db.SubCategory.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync(); //agar tidak error ketika update gagal pada sub category
                return View(MenuItemVM);
            }

            //untuk upload simpan gambar pada folder Images yang ada di wwwroot
            string webrootpath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFormDb = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);
            if (files.Count > 0)
            {
                //file sudah di upload
                var uploads = Path.Combine(webrootpath, "images"); //ambil dari folder images
                var extension_new = Path.GetExtension(files[0].FileName);

                //delete original file
                var imagePath = Path.Combine(webrootpath, menuItemFormDb.Image.TrimStart('\\')); //trim \ pertama pada field Image ex: \images\3.png
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                //we will upload the new file image
                using (var filesStream = new FileStream(Path.Combine(uploads, MenuItemVM.MenuItem.Id + extension_new), FileMode.Create))
                {
                    files[0].CopyTo(filesStream);
                }

                menuItemFormDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + extension_new;
            }

            menuItemFormDb.Name = MenuItemVM.MenuItem.Name;
            menuItemFormDb.Description = MenuItemVM.MenuItem.Description;
            menuItemFormDb.Price = MenuItemVM.MenuItem.Price;
            menuItemFormDb.Spicyness = MenuItemVM.MenuItem.Spicyness;
            menuItemFormDb.CategoryId = MenuItemVM.MenuItem.CategoryId;
            menuItemFormDb.SubCategoryId = MenuItemVM.MenuItem.SubCategoryId;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        //GET - DETAIL
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).SingleOrDefaultAsync(x => x.Id == id);
            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM); //langsung memanggil bind property yang sudah diberikan value pada constructor
        }

        //GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).SingleOrDefaultAsync(x => x.Id == id);

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM); //langsung memanggil bind property yang sudah diberikan value pada constructor
        }

        //POST - DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //delete image
            string webrootpath = _hostingEnvironment.WebRootPath;

            var menuItemFormDb = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);
            if (menuItemFormDb == null)
            {
                return View();
            }

            if (menuItemFormDb.Image != null)
            {
                //delete file
                var imagePath = Path.Combine(webrootpath, menuItemFormDb.Image.TrimStart('\\')); //trim \ pertama pada field Image ex: \images\3.png
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _db.MenuItem.Remove(menuItemFormDb);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
