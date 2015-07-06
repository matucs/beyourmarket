﻿using BeYourMarket.Core.Migrations;
using BeYourMarket.Web.Binders;
using BeYourMarket.Web.Migrations;
using BeYourMarket.Web.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using i18n;
using BeYourMarket.Web.Utilities;

namespace BeYourMarket.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //http://stackoverflow.com/questions/1718501/asp-net-mvc-best-way-to-trim-strings-after-data-entry-should-i-create-a-custo
            ModelBinders.Binders.Add(typeof(string), new TrimModelBinder());

            // Blacklist certain URLs from being 'localized'.
            i18n.UrlLocalizer.QuickUrlExclusionFilter = new System.Text.RegularExpressions.Regex(@"(?:sitemap\.xml|\.css|\.jpg|\.png|\.svg|\.woff|\.woff2|\.eot|\.js|\.html)$|(elmah|bundles)");

            //https://github.com/turquoiseowl/i18n#project-configuration
            // Change from the of temporary redirects during URL localization
            i18n.LocalizedApplication.Current.PermanentRedirects = false;

            // Change the URL localization scheme from Scheme1.
            i18n.UrlLocalizer.UrlLocalizationScheme = i18n.UrlLocalizationScheme.Scheme1;

            i18n.LocalizedApplication.Current.DefaultLanguage = BeYourMarket.Web.Utilities.LanguageHelper.DefaultCulture;

            // Use theme razor if database is installed
            if (ConnectionStringHelper.IsDatabaseInstalled())
            {
                //remove all view engines
                ViewEngines.Engines.Clear();
                //except the themeable razor view engine we use
                ViewEngines.Engines.Add(new ThemeableRazorViewEngine());
            }
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            //ensure database is installed            
            if (!ConnectionStringHelper.IsDatabaseInstalled())
            {
                HttpContextBase context = new HttpContextWrapper(HttpContext.Current);
                RouteData rd = RouteTable.Routes.GetRouteData(context);

                //http://stackoverflow.com/questions/16819585/get-absolute-url-path-of-an-action-from-within-global-asax
                // Check if the current controller is Install
                if (rd != null)
                {
                    string controllerName = rd.Values.ContainsKey("controller") ? rd.GetRequiredString("controller") : string.Empty;
                    string actionName = rd.Values.ContainsKey("action") ? rd.GetRequiredString("action") : string.Empty;

                    // check if it's bundles or content
                    if (!(controllerName.Equals("bundles") || controllerName.Equals("content")))
                    {
                        if (!controllerName.Equals("install", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Response.RedirectToRoute("Install");
                        }
                    }
                }
            }

            // Check if language from the url is enabled, if not, redirect to the default language
            var language = Context.GetPrincipalAppLanguageForRequest().GetLanguage();
            if (!LanguageHelper.AvailableLanguges.Languages.Any(x => x.Culture == language && x.Enabled))
            {
                var returnUrl = LocalizedApplication.Current.UrlLocalizerForApp.SetLangTagInUrlPath(
                    Request.RequestContext.HttpContext, Request.Url.AbsolutePath, UriKind.RelativeOrAbsolute, 
                    string.IsNullOrEmpty(LanguageHelper.DefaultCulture) ? null : LanguageHelper.DefaultCulture).ToString();

                Response.Redirect(returnUrl);
            }
        }
    }
}
