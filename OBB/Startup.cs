using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OBB.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Tweetinvi;
using System;
using System.Threading.Tasks;
using System.Linq;
using OBB.Services;

namespace OBB
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddTransient<ITwitterService,TwitterService>();
            var consumerKey = Configuration["Authentication:Twitter:ConsumerAPIKey"];
            var consumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];
            var appTokenKey = Configuration["Authentication:Twitter:AccessToken"];
            var appTokenSecret = Configuration["Authentication:Twitter:AccessSecret"];
            services.AddAuthentication()
                .AddTwitter(c => {
                    c.ConsumerKey = consumerKey;
                    c.ConsumerSecret = consumerSecret;
                    c.RetrieveUserDetails = true;
                    c.SaveTokens = true;
                    c.Events.OnCreatingTicket = oAuthCreatingTicketContext =>
                    {
                        var authenticationTokens = oAuthCreatingTicketContext.Properties.GetTokens().ToList();
                        var authenticationToken = new AuthenticationToken()
                        {
                            Name = "TicketCreated",
                            Value = DateTime.UtcNow.ToString()
                        };
                        authenticationTokens.Add(authenticationToken);
                        oAuthCreatingTicketContext.Properties.StoreTokens(authenticationTokens);
                        Auth.SetUserCredentials(consumerKey, consumerSecret, oAuthCreatingTicketContext.AccessToken, oAuthCreatingTicketContext.AccessTokenSecret);
                        return Task.CompletedTask;
                    };
                });
            services.AddTwitter(consumerKey, consumerSecret, appTokenKey, appTokenSecret);
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage()
                    .UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error")
                    .UseHsts();
            }
            app.UseHttpsRedirection()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
                    endpoints.MapRazorPages();
                });
        }
    }
}