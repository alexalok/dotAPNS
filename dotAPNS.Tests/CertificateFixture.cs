using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace dotAPNS.Tests
{
    public class CertificateFixture
    {
        const string EnvFileName = "env.json";
        const string EnvNamePrefix = "dotapns_tests_";

        public string P8CertPath { get; }
        public string P8CertData { get; }
        public X509Certificate2 P12Cert { get; }

        public CertificateFixture()
        {
            IEnumerable<JProperty> jsonProperties = null;
            if (File.Exists(EnvFileName))
            {
                string fileContents = File.ReadAllText(EnvFileName);
                var json = JObject.Parse(fileContents);
                jsonProperties = json.Properties();
            }

            string p8CertEnvName = $"{EnvNamePrefix}p8_path".ToLowerInvariant();
            P8CertPath = Environment.GetEnvironmentVariable(p8CertEnvName)
                ?? jsonProperties?.FirstOrDefault(p => string.Equals(p.Name, p8CertEnvName, StringComparison.InvariantCultureIgnoreCase))?.Value.Value<string>()
                ?? throw new InvalidOperationException($"Please set {p8CertEnvName} environment variable before running tests or use env.json file. " +
                    "See https://github.com/alexalok/dotAPNS/wiki/Unit-testing for reference.");

            string p8CertDataEnvName = $"{EnvNamePrefix}p8_contents".ToLowerInvariant();
            P8CertData = Environment.GetEnvironmentVariable(p8CertDataEnvName)
                    ?? jsonProperties?.FirstOrDefault(p => string.Equals(p.Name, p8CertDataEnvName, StringComparison.InvariantCultureIgnoreCase))?.Value.Value<string>()
                    ?? throw new InvalidOperationException($"Please set {p8CertDataEnvName} environment variable before running tests or use env.json file. " +
                        "See https://github.com/alexalok/dotAPNS/wiki/Unit-testing for reference.");

            string p12CertBase64EnvName = $"{EnvNamePrefix}p12_base64enc".ToLowerInvariant();
            string p12Base64 = Environment.GetEnvironmentVariable(p12CertBase64EnvName)
                ?? jsonProperties?.FirstOrDefault(p => string.Equals(p.Name, p12CertBase64EnvName, StringComparison.InvariantCultureIgnoreCase))?.Value.Value<string>()
                ?? throw new InvalidOperationException($"Please set {p12CertBase64EnvName} environment variable before running tests or use env.json file. " +
                    "See https://github.com/alexalok/dotAPNS/wiki/Unit-testing for reference.");
            var p12Bytes = Convert.FromBase64String(p12Base64);
            P12Cert = new X509Certificate2(p12Bytes);
        }
    }

    //[CollectionDefinition("certs")]
    //public class CertificateDataCollection : ICollectionFixture<CertificateFixture>
    //{
    //}
}