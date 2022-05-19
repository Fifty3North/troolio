using Troolio.Core;
using Troolio.Core.Client;

namespace Sample.Api.Controllers
{
    public abstract class BaseController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        protected readonly ITroolioClient _troolioClient;

        public BaseController(ITroolioClient troolioClient)
        {
            _troolioClient = troolioClient;
        }
        protected Metadata GetUserMetadata(Guid userId, Guid deviceId)
        {
            return new Metadata(Guid.NewGuid(), userId, deviceId);
        }
    }
}
