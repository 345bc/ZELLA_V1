using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ESHOPPER
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            // Xóa định dạng XML, chỉ trả về JSON
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Cấu hình JSON để bỏ qua vòng lặp (Loop)
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

            // (Tùy chọn) Làm đẹp JSON (thụt đầu dòng) để dễ đọc
            json.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
        }
    }
}
