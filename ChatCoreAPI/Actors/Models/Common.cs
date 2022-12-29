﻿using Akka.Actor;

namespace ChatCoreAPI.Actors.Models
{
    public class Common
    {
    }

    public class WSSendEvent
    {
        public string EventType { get; set; }

        public string ChannelId { get; set; }

        public string ChannelName { get; set; }

        public string EventData { get; set; }

    }

}
