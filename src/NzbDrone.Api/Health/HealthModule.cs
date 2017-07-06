using System.Collections.Generic;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Api.Health
{
    public class HealthModule : NzbDroneRestModuleWithSignalR<HealthResource, HealthCheck>,
                                IHandle<HealthCheckCompleteEvent>
    {
        private readonly IHealthCheckService _healthCheckService;

        public HealthModule(IHealthCheckService healthCheckService)
            : base()
        {
            _healthCheckService = healthCheckService;
            GetResourceAll = GetHealth;
        }

        private List<HealthResource> GetHealth()
        {
            return _healthCheckService.Results().ToResource();
        }

        public void Handle(HealthCheckCompleteEvent message)
        {
            BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
