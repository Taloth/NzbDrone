using NzbDrone.Api.REST;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Api
{
    public abstract class NzbDroneRestModuleWithSignalR<TResource, TModel> : NzbDroneRestModule<TResource>, IHandle<ModelEvent<TModel>>
        where TResource : RestResource, new()
        where TModel : ModelBase, new()
    {
        protected NzbDroneRestModuleWithSignalR()
        {

        }

        protected NzbDroneRestModuleWithSignalR(string resource)
            : base(resource)
        {

        }

        public void Handle(ModelEvent<TModel> message)
        {
            if (message.Action == ModelAction.Deleted || message.Action == ModelAction.Sync)
            {
                BroadcastResourceChange(message.Action);
            }

            BroadcastResourceChange(message.Action, message.Model.Id);
        }

        protected void BroadcastResourceChange(ModelAction action, int id)
        {
            var resource = GetResourceById(id);
            BroadcastResourceChange(action, resource);
        }


        protected void BroadcastResourceChange(ModelAction action, TResource resource)
        {
        }


        protected void BroadcastResourceChange(ModelAction action)
        {
        }
    }
}
