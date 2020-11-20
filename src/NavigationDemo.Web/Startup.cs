﻿using cloudscribe.Web.Localization;
using cloudscribe.Web.SiteMap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Linq;

namespace NavigationDemo.Web
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
            services.AddScoped<ISiteMapNodeService, NavigationTreeSiteMapNodeService>();
            services.AddCloudscribeNavigation(Configuration.GetSection("NavigationOptions"));

            services.AddScoped<cloudscribe.Web.Navigation.INavigationTreeBuilder, cloudscribe.Web.Navigation.JsonNavigationTreeBuilder>();

            // this was an experiment for https://github.com/cloudscribe/cloudscribe.Web.Navigation/issues/71
            //services.AddScoped<cloudscribe.Web.Navigation.INodeUrlPrefixProvider, CustomUrlPrefixProvider>();


            services.Configure<GlobalResourceOptions>(Configuration.GetSection("GlobalResourceOptions"));
            services.AddSingleton<IStringLocalizerFactory, GlobalResourceManagerStringLocalizerFactory>();
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });

            services.Configure<MvcOptions>(options =>
            {
                // options.InputFormatters.Add(new Xm)
                options.CacheProfiles.Add("SiteMapCacheProfile",
                     new CacheProfile
                     {
                         Duration = 100
                     });
                
            });

            services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization()
                .AddRazorOptions(options =>
            {

            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            ;

            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "application";
                options.DefaultChallengeScheme = "application";
            })
                .AddCookie("application", options =>
                {
                    options.LoginPath = new PathString("/FakeAccount/Index");
                    
                });

            var supportedCultures = new[]
                {
                     new CultureInfo("en-US"),
                     new CultureInfo("es-ES"),
                     new CultureInfo("fr-FR"),
                     new CultureInfo("de-DE"),
                     new CultureInfo("hi-IN"),  //Hindi
                     new CultureInfo("ru-RU"),
                     new CultureInfo("zh-Hans"), //Chinese Simplified
                     new CultureInfo("zh-Hant"), //Chinese Traditional
                };

            var routeSegmentLocalizationProvider = new FirstUrlSegmentRequestCultureProvider(supportedCultures.ToList());
            
            services.Configure<RequestLocalizationOptions>(options =>
            {
                

                // State what the default culture for your application is. This will be used if no specific culture
                // can be determined for a given request.
                options.DefaultRequestCulture = new RequestCulture(culture: "en-US", uiCulture: "en-US");

                // You must explicitly state which cultures your application supports.
                // These are the cultures the app supports for formatting numbers, dates, etc.
                options.SupportedCultures = supportedCultures;

                // These are the cultures the app supports for UI strings, i.e. we have localized resources for.
                options.SupportedUICultures = supportedCultures;

                // You can change which providers are configured to determine the culture for requests, or even add a custom
                // provider with your own logic. The providers will be asked in order to provide a culture for each request,
                // and the first to provide a non-null result that is in the configured supported cultures list will be used.
                // By default, the following built-in providers are configured:
                // - QueryStringRequestCultureProvider, sets culture via "culture" and "ui-culture" query string values, useful for testing
                // - CookieRequestCultureProvider, sets culture via "ASPNET_CULTURE" cookie
                // - AcceptLanguageHeaderRequestCultureProvider, sets culture via the "Accept-Language" request header
                //options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
                //{
                //  // My custom request culture logic
                //  return new ProviderCultureResult("en");
                //}));

                options.RequestCultureProviders.Insert(0, routeSegmentLocalizationProvider);

            });

            
            


            services.AddAuthorization(options =>
            {
               
                options.AddPolicy(
                    "AdminsPolicy",
                    authBuilder =>
                    {
                        authBuilder.RequireRole("Admins");
                    });

                options.AddPolicy(
                    "MembersPolicy",
                    authBuilder =>
                    {
                        authBuilder.RequireRole("Admins", "Members");
                    });

            });

        


    }


    

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env, 
            IOptions<RequestLocalizationOptions> locOptions
            )
        {
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
               
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization
            //https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx




            //var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);
            
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllerRoute(
                //    name: "default",
                //    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "areaRoute-localized",
                    pattern: "{culture}/{area:exists}/{controller}/{action}/{id?}",
                    defaults: new { action = "Index" },
                    constraints: new { culture = new CultureSegmentRouteConstraint() }
                    );

                endpoints.MapControllerRoute("areaRoute", "{area:exists}/{controller=Roswell}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                   name: "OverviewIndex",
                   pattern: "overview"
                   , defaults: new { controller = "Overview", action = "Index" }
                   );

                endpoints.MapControllerRoute(
                    name: "OverviewWhatever",
                    pattern: "whatever/overview"
                    , defaults: new { controller = "Whatever", action = "Overview" }
                    );




                endpoints.MapControllerRoute(
                    name: "default-localized",
                    pattern: "{culture}/{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" },
                    constraints: new { culture = new CultureSegmentRouteConstraint() }
                    );



                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");



                endpoints.MapRazorPages();
            });

            //app.UseMvc(routes =>
            //{
                

            //    routes.MapRoute(
            //        name: "areaRoute-localized", 
            //        template:"{culture}/{area:exists}/{controller}/{action}/{id?}",
            //        defaults: new {  action = "Index" },
            //        constraints: new { culture = new CultureSegmentRouteConstraint() }
            //        );

            //    routes.MapRoute("areaRoute", "{area:exists}/{controller=Roswell}/{action=Index}/{id?}");

            //    routes.MapRoute(
            //       name: "OverviewIndex",
            //       template: "overview"
            //       , defaults: new { controller = "Overview", action = "Index" }
            //       );

            //    routes.MapRoute(
            //        name: "OverviewWhatever",
            //        template: "whatever/overview"
            //        , defaults: new { controller = "Whatever", action = "Overview" }
            //        );

                


            //    routes.MapRoute(
            //        name: "default-localized",
            //        template: "{culture}/{controller}/{action}/{id?}",
            //        defaults: new { controller= "Home", action = "Index" },
            //        constraints: new { culture = new CultureSegmentRouteConstraint() }
            //        );



            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}");




            //});
        }
    }
}
