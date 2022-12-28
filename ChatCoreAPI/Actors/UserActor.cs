using Akka.Actor;
using Akka.Event;

using ChatCoreAPI.Actors.Models;
using ChatCoreAPI.Hubs;

using Microsoft.AspNetCore.SignalR;

namespace ChatCoreAPI.Actors
{
    public class UserActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private IActorRef _channelActor { get; set; }

        private readonly IServiceScopeFactory _scopeFactory;

        private string ConnectionId { get; set; }

        public UserActor(string connectionId, IServiceScopeFactory scopeFactory)
        {
            log.Info("Create UserActor: {0}", connectionId);

            _scopeFactory = scopeFactory;

            ConnectionId = connectionId;

            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });

            Receive<JoinChannel>(message => {
                log.Info("Received String message: {0}", message);

                var result = message.ChannelManagerActor.Ask(new ChannelInfo() { 
                    ChannelId = message.ChannelId                    
                }).Result;

                if (result is ChannelInfo)
                {
                    ChannelInfo channelInfo = result as ChannelInfo;
                    _channelActor = channelInfo.ChannelActor;

                    _channelActor.Tell(message);
                }
            });

            Receive<ChannelInfo>(async message => {
                log.Info("Received ChannelInfo message: {0}", message);
                await OnJoinChannel(message);
            });

            Receive<ErrorEventMessage>(async message => {
                log.Error($"Received ErrorEvent message: {message.ErrorCode} {message.ErrorMessage}");
                await OnErrorMessage(message);
            });


        }

        public async Task OnJoinChannel(ChannelInfo channelInfo)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Clients.Client(ConnectionId).SendAsync("OnJoinChannel", channelInfo.ChannelId, channelInfo.ChannelName);
            }
        }

        public async Task OnErrorMessage(ErrorEventMessage errorEvent)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Clients.Client(ConnectionId).SendAsync("OnErrorMessage", errorEvent.ErrorCode, errorEvent.ErrorMessage);
            }
        }


        public static Props Prop(string connectionId, IServiceScopeFactory scopeFactory)
        {
            return Akka.Actor.Props.Create(() => new UserActor(connectionId, scopeFactory));
        }

        protected override void PostStop()
        {
            log.Info("Stop UserActor: {0}", ConnectionId);
            base.PostStop();
        }
    }
}
