using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace dotAPNS.Benchmarks
{
    public class ApnsClientBenchmark
    {
        public readonly ApnsJwtOptions JwtOptions;
        private readonly ApnsClient apnsClient;
        private readonly List<string> tokens = new List<string>();

        public ApplePush ApplePush { get; set; } = new ApplePush(ApplePushType.Alert).SendToDevelopmentServer().AddAlert("Test", "Benchmark", "body");

        public ApnsClientBenchmark()
        {
            var configuration = new ConfigurationBuilder()
                                    .AddUserSecrets<ApnsClientBenchmark>()
                                    .Build();
            JwtOptions = configuration.GetSection(nameof(ApnsJwtOptions)).Get<ApnsJwtOptions>() ?? throw new InvalidDataException("Couldn't load the data");

            var httpClient = new HttpClient();

            httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            httpClient.DefaultRequestVersion = HttpVersion.Version20;
            httpClient.DefaultRequestHeaders.ConnectionClose = false;

            apnsClient = ApnsClient.CreateUsingJwt(new HttpClient(), JwtOptions);

            tokens.Add(configuration["Token"]);
        }

        [GlobalSetup]
        public void CreateNotification()
        {
            ApplePush.AddToken(tokens.First());
        }

        [Benchmark]
        public async Task SendNotification()
        {
            await apnsClient.SendAsync(ApplePush).ConfigureAwait(false);
        }

        [Benchmark]
        public void GeneratePayload()
        {
            ApplePush.GeneratePayload();
        }

    }
}
