using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;


namespace GameRecord
{
	public class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			// Web API configuration and services

			// Web API routes
			config.MapHttpAttributeRoutes();

			config.EnableCors();

			config.Routes.MapHttpRoute(
					name: "CallbackApi",
					routeTemplate: "api/callback/{controller}/{action}",
					defaults: new { id = RouteParameter.Optional }
			);

			config.Routes.MapHttpRoute(
					name: "DefaultApi",
					routeTemplate: "api/{controller}/{action}",
					defaults: new { id = RouteParameter.Optional }
			);

			
		}
	}
}