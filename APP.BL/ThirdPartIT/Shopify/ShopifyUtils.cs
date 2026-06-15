//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace APP.BL.ThirdPartIT.Shopify
//{
    

//    public static class ShopifyUtils
//    {
//        private static IDictionary<string, string> _dotEnvFile;

//        static ShopifyUtils()
//        {
//            // Console.WriteLine("DIRECTORY: " + System.IO.Directory.GetCurrentDirectory());
//            // dotEnvFile = DotEnvFile.DotEnvFile.LoadFile("env.yml");
//        }

//        /// <summary>
//        /// Attempts to get an environment variable first by the key, then by 'SHOPIFYSHARP_{KEY}'. All keys must be uppercased!
//        /// </summary>
//        private static string Get(string key)
//        {
//            key = key.ToUpper();

//            if (_dotEnvFile == null && System.IO.File.Exists("./env.yml"))
//            {
//                _dotEnvFile = DotEnvFile.LoadFile("./env.yml", true);
//            }

//            if (_dotEnvFile != null && _dotEnvFile.ContainsKey(key))
//            {
//                return _dotEnvFile[key];
//            }

//            var prefix = "SHOPIFYSHARP_";
//            var value = Environment.GetEnvironmentVariable(key) ?? Environment.GetEnvironmentVariable(prefix + key);

//            if (string.IsNullOrEmpty(value))
//            {
//                throw new Exception($"{nameof(key)} {key} was not found in environment variables. Add the key or {prefix + key} to your environment variables and try again.");
//            }

//            return value;
//        }

//        public static string ApiKey => Get("API_KEY");

//        public static string SecretKey => Get("SECRET_KEY");

//        public static string AccessToken => Get("ACCESS_TOKEN");

//        public static string MyShopifyUrl => Get("MY_SHOPIFY_URL");
//    }
//}
