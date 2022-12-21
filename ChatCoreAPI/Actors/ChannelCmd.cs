using System.Text.Json;

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

    public class CreateChannel : ChannelCmd
    {
        public string ChannelName { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

}
