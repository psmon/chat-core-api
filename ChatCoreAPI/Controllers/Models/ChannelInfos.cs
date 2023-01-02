namespace ChatCoreAPI.Controllers.Models
{
    public class ChannelInfo
    {
        public string ChannelId { get; set; }

        public string ChannelName { get; set; }
    }
    public class ChannelInfos
    {
        public List<ChannelInfo> channelInfos { get; set; }
    }
}
