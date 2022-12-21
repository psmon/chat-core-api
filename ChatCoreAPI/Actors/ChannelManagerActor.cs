using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Event;

namespace ChatCoreAPI.Actors
{
    public class ChannelManagerActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        public ChannelManagerActor()
        {
            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });

            Receive<CreateChannel>(message => {
                log.Info("Received CreateChannel message: {0}", message);
                               
                Context.ActorOf(ChannelActor.Prop(message.ChannelName), message.ChannelName);
                //Sender.Tell(message);
            });
            
        }

        public static Props Prop()
        {
            return Akka.Actor.Props.Create(() => new ChannelManagerActor());
        }
    }
}
