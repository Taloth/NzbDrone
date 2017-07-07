using Nancy;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Http;

namespace NzbDrone.Api
{
    public class NzbDroneTestModule : NancyModule
    {
        private readonly IHttpClient _httpClient;

        public NzbDroneTestModule(IHttpClient httpClient)
            : base("/test")
        {
            _httpClient = httpClient;

            Get["/"] = _ => DoTest();
        }

        private Response DoTest()
        {
            var requestBuilder = new HttpRequestBuilder("https://torrentapi.org")
                           .WithRateLimit(3.0)
                           .Resource("/pubapi_v2.php?get_token=get_token&app_id=Sonarr")
                           .Accept(HttpAccept.Json);

            var response = _httpClient.Get<JObject>(requestBuilder.Build());

            return Response.AsText(response.ToString());
        }
    }
}
