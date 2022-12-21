using Akka.Actor;
using Akka.Event;

namespace ChatCoreAPI.Actors
{
    public class ChannelActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        public ChannelActor()
        {
            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });
            
        }

        public static Props Prop()
        {
            return Akka.Actor.Props.Create(() => new ChannelActor());
        }
    }
}
