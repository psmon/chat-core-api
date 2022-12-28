using Akka.Actor;
using Akka.Event;

using ChatCoreAPI.Actors.Models;

namespace ChatCoreAPI.Actors
{
    public class ChannelActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private ChannelInfo ChannelInfo { get; set; }

        public ChannelActor(CreateChannel channelInfo)
        {
            log.Info("Create ChannelActor: {0}", channelInfo.ChannelName);
            ChannelInfo = channelInfo;

            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });

            Receive<JoinChannel>(message => {
                if (message.ChannelId == ChannelInfo.ChannelId)
                {
                    log.Info("Received String message: {0}", message);                    
                    Sender.Tell(ChannelInfo);
                }
                else
                {
                    Sender.Tell(new ErrorEvent() { ErrorCode = -401,ErrorMessage = "로그인Error" });
                }
            });

        }

        public static Props Prop(CreateChannel channelInfo)
        {
            return Akka.Actor.Props.Create(() => new ChannelActor(channelInfo));
        }
    }
}
