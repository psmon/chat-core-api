using Akka.Actor;
using Akka.Event;

namespace ChatCoreAPI.Actors
{
    public class UserActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private string ConnectionId { get; set; }

        public UserActor(string connectionId)
        {
            log.Info("Create UserActor: {0}", connectionId);

            ConnectionId = connectionId;

            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });
            
        }

        public static Props Prop(string connectionId)
        {
            return Akka.Actor.Props.Create(() => new UserActor(connectionId));
        }
    }
}
