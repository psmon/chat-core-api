using Akka.Actor;
using Akka.Event;

using ChatCoreAPI.Actors.Models;

namespace ChatCoreAPI.Actors
{
    public class UserActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private IActorRef _channelActor { get; set; }

        private string ConnectionId { get; set; }

        public UserActor(string connectionId)
        {
            log.Info("Create UserActor: {0}", connectionId);

            ConnectionId = connectionId;

            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });

            Receive<JoinChannel>(message => {
                log.Info("Received String message: {0}", message);

                var result = message.ChannelManagerActor.Ask(new ChannelInfo()).Result;

                if (result is ChannelInfo)
                {
                    ChannelInfo channelInfo = result as ChannelInfo;
                    _channelActor = channelInfo.ChannelActor;

                    _channelActor.Tell(message);
                }
            });


        }

        public static Props Prop(string connectionId)
        {
            return Akka.Actor.Props.Create(() => new UserActor(connectionId));
        }
    }
}
