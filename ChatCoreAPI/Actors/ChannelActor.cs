using Akka.Actor;
using Akka.Event;

using RoundRobin;

namespace ChatCoreAPI.Actors
{
    public class ChannelActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private ChannelInfo ChannelInfo { get; set; }

        private List<IActorRef> ActorRefs = new List<IActorRef>();

        private RoundRobinList<IActorRef> Router;

        int curIdx = 0;

        public ChannelActor(CreateChannel channelInfo)
        {
            log.Info("Create ChannelActor: {0}", channelInfo.ChannelName);
            ChannelInfo = channelInfo;

            Router = new RoundRobinList<IActorRef>(ActorRefs);

            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                if (message == "AutoAssignTest")
                {
                    var nextActor = NextActor();
                    if (nextActor != null)
                    {
                        nextActor.Tell(new AutoAssign() { RoomSession = Guid.NewGuid().ToString() });
                    }
                }
            });

            Receive<JoinChannel>(message => {
                log.Info("Received String message: {0}", message);
                if (message.ChannelId == ChannelInfo.ChannelId)
                {                    
                    Sender.Tell(ChannelInfo);
                    ActorRefs.Add(Sender);
                    Router = new RoundRobinList<IActorRef>(ActorRefs);

                }
                else
                {
                    Sender.Tell(new ErrorEventMessage() { ErrorCode = -401,ErrorMessage = "로그인Error" });
                }
            });

            Receive<LeaveChannel>(message => {
                log.Info("Received LeaveChannel message: {0} {1}", message.ChannelId, message.ConnectionId);
                ActorRefs.Remove(Sender);
                Router = new RoundRobinList<IActorRef>(ActorRefs);
            });

        }

        public static Props Prop(CreateChannel channelInfo)
        {
            return Akka.Actor.Props.Create(() => new ChannelActor(channelInfo));
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
    }
}
