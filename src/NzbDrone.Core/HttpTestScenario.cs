using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Api
{
    public class HttpTestScenario : IHandle<ApplicationStartedEvent>
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public HttpTestScenario(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public void Handle(ApplicationStartedEvent message)
        {
            ThreadPool.QueueUserWorkItem(DoTest);
        }

        private void DoTest(object state)
        {
            _logger.Info("Test in 10 sec");

            Thread.Sleep(10000);
            _logger.Info("Starting Test");

            var requestBuilder = new HttpRequestBuilder("https://torrentapi.org")
                           .WithRateLimit(3.0)
                           .Resource("/pubapi_v2.php?get_token=get_token&app_id=Sonarr")
                           .Accept(HttpAccept.Json);

            var response = _httpClient.Get<JObject>(requestBuilder.Build());

            _logger.Info("Finished Test: " + response.ToString());


        }
    }
}
