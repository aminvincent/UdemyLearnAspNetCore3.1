using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using SpiceWeb.Mvc.Core.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using SpiceWeb.Mvc.Core.Service;
using SpiceWeb.Mvc.Core.Utility;
using Stripe;

namespace SpiceWeb.Mvc.Core
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            //dalam .net core 3.1 seperti berikut
            //services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
            //sedangkan di .net 3.0 tidak ada options.SignIn.RequireConfirmedAccount = true

            //menggunakan AddIdentity karena menggunakan custom page resigter agar tidak error
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddDefaultTokenProviders() //berfungsi untuk mendapatkan token ketika user lupa password
                .AddEntityFrameworkStores<ApplicationDbContext>();

            //tambahkan stripe online payement agar membaca pengaturan dari appsettings.json
            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));

            //tambahan untuk config send grid email
            services.AddSingleton<IEmailSender, EmailSender>();
            services.Configure<EmailOptions>(Configuration); //tanpa section ada di appsetting.json

            services.AddControllersWithViews();

            services.AddSingleton<IEmailSender, EmailSender>(); //menambahkan singleton pada EmailSender agar tidak error dan hanya menjadikan EmailSender menjadi 1 object

            //service ini digunakan untuk handle Authorization ketika User yang tidak berhak mengakses Controller 
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = $"/Identity/Account/Login";
                options.LogoutPath = $"/Identity/Account/Logout";
                options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
            });

            //original sebelum menggunakan Runtime Compilation
            //services.AddRazorPages();

            //login into Facebook => for detail info https://developers.facebook.com/
            services.AddAuthentication().AddFacebook(facebookOptions => 
            {
                facebookOptions.AppId = "3722014157883055";
                facebookOptions.AppSecret = "d59d579b5c8f798bc5d9099628d6061f";
            });

            //menambahkan session yang digunakan untuk menghitung jumlah shopping cart
            services.AddSession(options =>
            {
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(30); //set hanya 30 menit.
                options.Cookie.HttpOnly = true;
            });

            //menggunakan runtime compilation agar project auto refresh/compile ketika ada perubahan di codingan
            services.AddRazorPages().AddRazorRuntimeCompilation();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            //menambahkan stripe setting yg sudah diatur di method ConfigureServices
            StripeConfiguration.ApiKey = Configuration.GetSection("Stripe")["SecretKey"];
            //DotNET Core 2.2 StripeConfiguration.SetApiKey(Configuration.GetSection("Stripe")["SecretKey"]);

            app.UseSession(); //tambahakn ini agar session yg sudah di tambahkan di ConfigureServices bisa berfungsi

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
