using System.Collections.Generic;
using NzbDrone.Common.Composition;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http.Dispatchers;

namespace NzbDrone.Host
{
    public class MainAppContainerBuilder : ContainerBuilderBase
    {
        public static IContainer BuildContainer(StartupContext args)
        {
            var assemblies = new List<string>
                             {
                                 "NzbDrone.Host",
                                 "NzbDrone.Core"
                             };

            return new MainAppContainerBuilder(args, assemblies).Container;
        }

        private MainAppContainerBuilder(StartupContext args, List<string> assemblies)
            : base(args, assemblies)
        {
            Container.Register<IHttpDispatcher, FallbackHttpDispatcher>();
        }
    }
}
