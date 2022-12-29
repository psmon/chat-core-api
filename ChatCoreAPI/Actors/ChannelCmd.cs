using System.Text.Json;

using Akka.Actor;

namespace ChatCoreAPI.Actors
{
    public class ChannelCmd
    {
        public string EventId { get; set; }

        public ChannelCmd()
        {
            EventId = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class ChannelInfo : ChannelCmd
    {
        public string ChannelName { get; set; }

        public string ChannelId { get; set; }

        public IActorRef ChannelActor;
    }

    public class CreateChannel : ChannelInfo
    {
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }


    public class ContainActorInfo
    {
        public IActorRef ChannelManagerActor;
    }

    public class JoinChannel : ContainActorInfo
    {
        public string ChannelId { get; set; }

        public string AccessToken { get; set; }

        public string LoginId { get; set; }
    }

    public class NotyJoinChannel : JoinChannel
    {        
    }

    public class LeaveChannel
    {
        public string ChannelId { get; set; }

        public string ConnectionId { get; set; }
    }

    public class AutoAssign
    {
        public string RoomSession { get; set; }
    }

    public class ErrorEventMessage
    {
        public int ErrorCode {get; set; }

        public string ErrorMessage {get; set; }
    }
        

}
