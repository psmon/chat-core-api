using System.Text.Json;

using Akka.Actor;

namespace ChatCoreAPI.Actors
{
    public class ChannelCmd
    {
        public string EventId { get; set; }

        public string ConnectionId { get; set; }

        public IActorRef Target { get; set; }

        public IActorRef ChannelManagerActor { get; set; }

        public ChannelCmd()
        {
            EventId = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            return $"[{EventId}]::{ConnectionId}";
        }
    }

    public class AutoAsign
    {
        public string ChannelId { get; set; }
        public string AsignData {get; set; }

    }

    public class ChannelInfo : ChannelCmd
    {
        public string ChannelName { get; set; }

        public string ChannelId { get; set; }

        public IActorRef ChannelActor;

        public override string ToString()
        {
            return $"[{EventId}]::{ConnectionId}::{ChannelId}::{ChannelId}";
        }

    }

    public class CreateChannel : ChannelInfo
    {

    }

    public class DeleteChannel : ChannelInfo
    {
    }

    public class PrintChannelInfo
    {

    }


    public class JoinChannel : ChannelCmd
    {
        public string ChannelId { get; set; }

        public string AccessToken { get; set; }

        public string LoginId { get; set; }
    }


    public class NotyJoinChannel : JoinChannel
    {        
    }

    public class LeaveChannel : ChannelCmd
    {
        public string ChannelId { get; set; }
        
    }

    public class AutoAssignInfo : ChannelCmd
    {
        public string AsignData { get; set; }
    }

    public class ErrorEventMessage : ChannelCmd
    {
        public int ErrorCode {get; set; }

        public string ErrorMessage {get; set; }

    }
        

}
