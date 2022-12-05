using System;
using System.Collections.Generic;

using System.IO;
using YamlDotNet.Serialization.NamingConventions;

namespace MyShared
{
    public class ConfigSettings
    {
        public string description { get; set; }
        public string isProduction { get; set; }
        public string debug { get; set; }
        public string logToFile { get; set; }
        public string displayLogs { get; set; }
        public string logDir { get; set; }
        public IDictionary<string, mysqlConfig> mysql { get; set; }
        public List<int> testSites { get; set; }
    }
    public class mysqlConfig
    {
        public string host { get; set; }
        public int port { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public database database { get; set; }
    }

    public class database
    {
        public string defaultDb { get; set; }
        public string instancesDb { get; set; }
    }

    static class AppConfig
    {
        public static ConfigSettings Settings()
        {
            var config = new ConfigSettings();
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .Build();
            try
            {
                var myConfig = deserializer.Deserialize<ConfigSettings>(File.ReadAllText("./.config.yml"));
                return myConfig;
            }
            catch (System.Exception error)
            {
                Console.WriteLine(error);
                throw error;
            }
        }

    }

}