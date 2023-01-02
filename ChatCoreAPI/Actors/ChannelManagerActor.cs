using Akka.Actor;
using Akka.Event;

using ChatCoreAPI.Controllers.Models;

namespace ChatCoreAPI.Actors
{
    public class ChannelManagerActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private Dictionary<string,ChannelInfo> channels = new Dictionary<string,ChannelInfo>();

        public ChannelManagerActor()
        {
            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });            

            Receive<CreateChannel>(message => {
                try
                {
                    log.Info("Received CreateChannel message: {0}", message);
                    Context.ActorOf(ChannelActor.Prop(message), message.ChannelId);
                    Sender.Tell("ok");
                    channels[message.ChannelId] = message as ChannelInfo;
                }
                catch (Exception ex)
                {
                    Sender.Tell(new ErrorEventMessage()
                    {
                        ErrorCode = -1,
                        ErrorMessage = $"{message.ChannelId} 채널생성실패 - {ex.Message}"
                    });
                }                

            });

            Receive<DeleteChannel>(message => {
                try
                {
                    log.Info("Received DeleteChannel message: {0}", message);
                    var channelActor = Context.ActorSelection(message.ChannelId).ResolveOne(TimeSpan.FromSeconds(1)).Result;
                    channelActor.Tell(PoisonPill.Instance);
                    Sender.Tell("ok");
                    channels.Remove(message.ChannelId);
                }
                catch (Exception ex)
                {
                    Sender.Tell(new ErrorEventMessage()
                    {
                        ErrorCode = -1,
                        ErrorMessage = $"{message.ChannelId} 채널삭제실패 - {ex.Message}"
                    });
                }
            });

            Receive<ChannelInfo>(message => {
                try
                {
                    log.Info("Received ChannelInfo message: {0}", message);
                    var channelActor = Context.ActorSelection(message.ChannelId).ResolveOne(TimeSpan.FromSeconds(1)).Result;
                    if (channelActor != null)
                    {
                        ChannelInfo channelInfo = new ChannelInfo()
                        {
                            ChannelName = message.ChannelName,
                            ChannelId = message.ChannelId,
                            ChannelActor = channelActor
                        };
                        Sender.Tell(channelInfo);
                    }
                }
                catch (Exception ex)
                {                    
                    log.Error(ex.Message);

                    Sender.Tell(new ErrorEventMessage() { 
                        ErrorCode = -1,
                        ErrorMessage = $"{message.ChannelId} 채널을 찾을수 없습니다."                        
                    });
                }
            });

            Receive<PrintChannelInfo>(message => {
                log.Info("Received PrintChannelInfo message: {0}", message);

                ChannelInfos channelInfos = new ChannelInfos();
                channelInfos.channelInfos = new List<Controllers.Models.ChannelInfo>();

                foreach (var channerlInfo in channels.Values)
                {
                    channelInfos.channelInfos.Add(new Controllers.Models.ChannelInfo()
                    {
                        ChannelId = channerlInfo.ChannelId,
                        ChannelName =  channerlInfo.ChannelName,
                    });
                }
                Sender.Tell(channelInfos);
            });

        }

        public static Props Prop()
        {
            return Akka.Actor.Props.Create(() => new ChannelManagerActor());
        }
    }
}
