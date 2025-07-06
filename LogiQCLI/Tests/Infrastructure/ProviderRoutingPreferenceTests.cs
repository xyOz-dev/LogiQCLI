using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Services;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;

namespace LogiQCLI.Tests.Infrastructure
{
    public class ProviderRoutingPreferenceTests : TestBase
    {
        public override string TestName => "Provider Preferences – Live Endpoint Routing";

        private const string SampleEndpointJson = @"{
  ""data"": {
    ""id"": ""anthropic/claude-sonnet-4"",
    ""name"": ""Anthropic: Claude Sonnet 4"",
    ""created"": 1747930371,
    ""description"": ""test"",
    ""architecture"": {
      ""tokenizer"": ""Claude"",
      ""modality"": ""text"",
      ""input_modalities"": [ ""text"" ],
      ""output_modalities"": [ ""text"" ]
    },
    ""endpoints"": [
      {
        ""name"": ""Google | anthropic/claude-4-sonnet-20250522"",
        ""context_length"": 200000,
        ""pricing"": {
          ""prompt"": ""0.000003"",
          ""completion"": ""0.000015"",
          ""request"": ""0""
        },
        ""provider_name"": ""Google"",
        ""tag"": ""google-vertex"",
        ""quantization"": null,
        ""supported_parameters"": [ ""max_tokens"", ""temperature"", ""tools"" ]
      },
      {
        ""name"": ""Anthropic | anthropic/claude-4-sonnet-20250522"",
        ""context_length"": 200000,
        ""pricing"": {
          ""prompt"": ""0.000003"",
          ""completion"": ""0.000015"",
          ""request"": ""0""
        },
        ""provider_name"": ""Anthropic"",
        ""tag"": ""anthropic"",
        ""quantization"": null,
        ""supported_parameters"": [ ""max_tokens"", ""temperature"", ""tools"" ]
      }
    ]
  }
}";

        public override async Task<TestResult> ExecuteAsync()
        {
            try
            {
                var handler = new FakeHandler(SampleEndpointJson);
                var httpClient = new HttpClient(handler);
                var manager = new ProviderPreferencesService(httpClient);

                var request = new ChatRequest
                {
                    Model = "anthropic/claude-sonnet-4",
                    Tools = new LogiQCLI.Tools.Core.Objects.Tool[]
                    {
                        new LogiQCLI.Tools.Core.Objects.Tool{ Type="function" }
                    }
                };

                var provider = await manager.BuildProviderPreferencesAsync(request);

                if (provider.Only == null || provider.Only.Length != 2)
                {
                    return TestResult.CreateFailure(TestName, $"Expected 2 provider tags in 'only' but got {(provider.Only == null ? 0 : provider.Only.Length)}", TimeSpan.Zero);
                }

                if (!provider.Only.Contains("google-vertex") || !provider.Only.Contains("anthropic"))
                {
                    return TestResult.CreateFailure(TestName, "Returned provider tags do not match expected", TimeSpan.Zero);
                }

                if (!string.Equals(provider.Sort, "price", StringComparison.OrdinalIgnoreCase))
                {
                    return TestResult.CreateFailure(TestName, "Expected sort=price by default", TimeSpan.Zero);
                }

                return TestResult.CreateSuccess(TestName, TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure(TestName, ex.ToString(), TimeSpan.Zero);
            }
        }

        private class FakeHandler : HttpMessageHandler
        {
            private readonly string _json;
            public FakeHandler(string json)
            {
                _json = json;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_json, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }
    }
} 