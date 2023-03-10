
using Akka.Actor;
using Akka.Event;
using Akka.Routing;

using ChatCoreAPI.Actors.Models;
using ChatCoreAPI.Hubs;

using Microsoft.AspNetCore.SignalR;

using RoundRobin;

namespace ChatCoreAPI.Actors
{
    public class ChannelActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private ChannelInfo ChannelInfo { get; set; }

        private List<IActorRef> ActorRefs = new List<IActorRef>();

        private Dictionary<string, IActorRef> ActorsByConncteID = new Dictionary<string, IActorRef>();

        private RoundRobinList<IActorRef> Router;

        private IActorRef RouterGroup;

        private readonly IServiceScopeFactory _scopeFactory;

        int curIdx = 0;

        public ChannelActor(CreateChannel channelInfo, IServiceScopeFactory scopeFactory)
        {
            log.Info("Create ChannelActor: {0}", channelInfo.ChannelName);
            
            ChannelInfo = channelInfo;

            _scopeFactory = scopeFactory;

            Router = new RoundRobinList<IActorRef>(ActorRefs);

            RouterUpdate(true);

            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                if (message == "AutoAssignTest")
                {
                    var nextActor = NextActor();
                    if (nextActor != null)
                    {
                        nextActor.Tell(new AutoAssignInfo() { AsignData = Guid.NewGuid().ToString() });
                    }
                }
            });

            Receive<AutoAsign>(message => {
                log.Info("Received AutoAsign message: {0}", message);
                
                // using AkkaRouter
                RouterGroup.Tell(new AutoAssignInfo() { AsignData = message.AsignData });

                // using Roudbin
                //var nextActor = NextActor();
                //if (nextActor != null)
                //{
                //   nextActor.Tell(new AutoAssignInfo() { AsignData = message.AsignData });
                //}
            });            

            ReceiveAsync<SendAllGroup>(async message => {
                log.Info("Received SendAllGroup message: {0}", message);
                if (!string.IsNullOrEmpty(message.SubGroup))
                {
                    await SendToSubGroup(message.ChannelId, message.SubGroup, message);
                }
                else
                {
                    await SendToGroup(message.ChannelId, message);
                }
            });

            Receive<SendSomeOne>(async message => {
                log.Info("Received String message: {0}", message);
                IActorRef _userActor;
                bool isGet = ActorsByConncteID.TryGetValue(message.ChannelId, out _userActor);
                if (isGet) _userActor.Tell(message);
            });

            Receive<JoinChannel>(message => {
                log.Info("Received String message: {0}", message);
                if (message.ChannelId == ChannelInfo.ChannelId)
                {                    
                    Sender.Tell(ChannelInfo);
                    ActorRefs.Add(Sender);
                    ActorsByConncteID[message.ConnectionId] = Sender;
                    Router = new RoundRobinList<IActorRef>(ActorRefs);

                    RouterUpdate(false);

                }
                else
                {
                    Sender.Tell(new ErrorEventMessage() { ErrorCode = -401,ErrorMessage = "로그인Error" });
                }
            });

            Receive<LeaveChannel>(message => {
                log.Info("Received LeaveChannel message: {0} {1}", message.ChannelId, message.ConnectionId);
                ActorRefs.Remove(Sender);
                ActorsByConncteID.Remove(message.ConnectionId);
                
                Router = new RoundRobinList<IActorRef>(ActorRefs);
                RouterUpdate(false);


                Sender.Tell("Ok-LeaveChannel");

            });

        }

        public async Task SendToGroup(string ChannelId, WSSendEvent wSSendEvent)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                await wsHub.Clients.Group(ChannelId).SendAsync("ReceiveMessage",
                    wSSendEvent.EventType, wSSendEvent.ChannelId, wSSendEvent.ChannelName, wSSendEvent.EventData);
            }
        }

        public async Task SendToSubGroup(string ChannelId, string subGroup, WSSendEvent wSSendEvent)
        {            
            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                await wsHub.Clients.Group(ChannelId + "-" + subGroup).SendAsync("ReceiveMessage",
                    wSSendEvent.EventType, wSSendEvent.ChannelId, wSSendEvent.ChannelName, wSSendEvent.EventData);
            }
        }

        public void RouterUpdate(bool isfirst)
        {
            if (!isfirst)
            {                
                RouterGroup.Tell(PoisonPill.Instance);
            }            
            RouterGroup = Context.ActorOf(Props.Empty.WithRouter(new RoundRobinGroup(ActorRefs)));
        }


        public static Props Prop(CreateChannel channelInfo, IServiceScopeFactory scopeFactory)
        {
            return Akka.Actor.Props.Create(() => new ChannelActor(channelInfo, scopeFactory));
        }

        public IActorRef NextActor()
        {            
            if (ActorRefs.Count > 0)
            {
                return Router.Next();
            }
            else
            {
                return null;
            }
        }

        protected override void PostStop()
        {
            log.Info("Stop ChannelActor: {0}", ChannelInfo.ChannelId);
            base.PostStop();
        }
    }
}
