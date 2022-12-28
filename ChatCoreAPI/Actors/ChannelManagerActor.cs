using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Event;

using ChatCoreAPI.Actors.Models;

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
                               
                Context.ActorOf(ChannelActor.Prop(message), message.ChannelId);
                //Sender.Tell(message);
            });

            Receive<ChannelInfo>(message => {
                log.Info("Received ChannelInfo message: {0}", message);
                var channelActor = Context.Child(message.ChannelId);

                if (channelActor != null)
                {
                    ChannelInfo channelInfo = new ChannelInfo()
                    {
                        ChannelName = message.ChannelName,
                        ChannelId = message.ChannelId,
                        ChannelActor = channelActor
                    };
                    Sender.Tell(message);
                }
                else
                {
                    Sender.Tell(new ErrorEvent() { ErrorCode= -1, ErrorMessage= "채널을 찾을수 없습니다." });
                }
            });

            //

        }

        public static Props Prop()
        {
            return Akka.Actor.Props.Create(() => new ChannelManagerActor());
        }
    }
}
