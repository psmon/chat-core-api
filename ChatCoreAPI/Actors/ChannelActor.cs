using Akka.Actor;
using Akka.Event;

namespace ChatCoreAPI.Actors
{
    public class ChannelActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private string ChannelName { get; set; }

        public ChannelActor(string channelName)
        {
            log.Info("Create ChannelActor: {0}", channelName);

            ChannelName = channelName;

            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });
            
        }

        public static Props Prop(string channelName)
        {
            return Akka.Actor.Props.Create(() => new ChannelActor(channelName));
        }
    }
}
