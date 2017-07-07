using System;
using System.ServiceProcess;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Host
{
    public interface INzbDroneServiceFactory
    {
        ServiceBase Build();
        void Start();
    }

    public class NzbDroneServiceFactory : ServiceBase, INzbDroneServiceFactory, IHandle<ApplicationShutdownRequested>
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly IStartupContext _startupContext;
        private readonly IBrowserService _browserService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public NzbDroneServiceFactory(IConfigFileProvider configFileProvider,
                                      IRuntimeInfo runtimeInfo,
                                      IStartupContext startupContext,
                                      IBrowserService browserService,
                                      IEventAggregator eventAggregator,
                                      Logger logger)
        {
            _configFileProvider = configFileProvider;
            _runtimeInfo = runtimeInfo;
            _startupContext = startupContext;
            _browserService = browserService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        public void Start()
        {
            if (OsInfo.IsNotWindows)
            {
                Console.CancelKeyPress += (sender, eventArgs) => LogManager.Configuration = null;
            }

            _runtimeInfo.IsExiting = false;

            _eventAggregator.PublishEvent(new ApplicationStartedEvent());

            if (!_startupContext.Flags.Contains(StartupContext.NO_BROWSER)
                && _configFileProvider.LaunchBrowser)
            {
                _browserService.LaunchWebUI();
            }
        }

        protected override void OnStop()
        {
            Shutdown();
        }

        public ServiceBase Build()
        {
            return this;
        }

        private void Shutdown()
        {
            _logger.Info("Attempting to stop application.");
            _logger.Info("Application has finished stop routine.");
            _runtimeInfo.IsExiting = true;
        }

        public void Handle(ApplicationShutdownRequested message)
        {
            if (!_runtimeInfo.IsWindowsService)
            {
                if (message.Restarting)
                {
                    _runtimeInfo.RestartPending = true;
                }

                LogManager.Configuration = null;
                Shutdown();
            }
        }
    }
}
