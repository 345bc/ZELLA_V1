using Newtonsoft.Json.Linq;
using System.IO;
using System.Web;

namespace ESHOPPER.Helpers
{
    public static class JwtConfig
    {
        public static string Key { get; private set; }
        public static string Issuer { get; private set; }
        public static string Audience { get; private set; }
        public static int ExpireMinutes { get; private set; }

        static JwtConfig()
        {
            var path = HttpContext.Current.Server.MapPath("~/appsettings.json");
            var json = File.ReadAllText(path);
            var jObj = JObject.Parse(json);

            Key = jObj["Jwt"]["Key"].ToString();
            Issuer = jObj["Jwt"]["Issuer"].ToString();
            Audience = jObj["Jwt"]["Audience"].ToString();
            ExpireMinutes = int.Parse(jObj["Jwt"]["ExpireMinutes"].ToString());
        }
    }
}
