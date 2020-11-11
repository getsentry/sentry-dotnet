using Microsoft.AspNetCore.Mvc;

namespace Sentry.Samples.Aws.Lambda.AspNetCoreServer
{
    [Route("api/[controller]")]
    public class BadController
    {
        [HttpGet]
        public string Get() => throw null;
    }
}
