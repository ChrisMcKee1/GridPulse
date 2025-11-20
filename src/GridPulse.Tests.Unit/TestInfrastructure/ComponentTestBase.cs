using System.Net;
using System.Net.Http;
using Bunit;
using GridPulse.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GridPulse.Tests.Unit.TestInfrastructure;

public abstract class ComponentTestBase : TestContext
{
    private readonly StubHttpMessageHandler _handler = new();

    protected ComponentTestBase()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://localhost")
        };

        Services.AddSingleton(httpClient);
        Services.AddScoped(sp => new GridPulseApiClient(sp.GetRequiredService<HttpClient>()));
    }

    protected void EnqueueHttpResponse(HttpResponseMessage response)
    {
        _handler.Enqueue(response);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public void Enqueue(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}