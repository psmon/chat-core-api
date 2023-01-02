using ChatCoreAPI.Actors;
using ChatCoreAPI.Controllers.Models;

using Microsoft.AspNetCore.Mvc;

using ChannelInfo = ChatCoreAPI.Actors.ChannelInfo;

namespace ChatCoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChannelManagerController
    {
        private IActorBridge _channelManagerActor;

        private IServiceScopeFactory _serviceScopeFactory;

        public ChannelManagerController(IActorBridge actorBridge, IServiceScopeFactory serviceScopeFactory)
        {
            _channelManagerActor = actorBridge;

            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpGet]
        public async Task<ChannelInfos> GetChannelInfo()
        {            
            var channelInfos = await _channelManagerActor.Ask<object>(new PrintChannelInfo());
            return channelInfos as ChannelInfos;
        }

        [HttpPost]
        public async Task<string> CreateChannel(string channelId, string channelName)
        {
            string testResult = "";
            var result = await _channelManagerActor.Ask<object>(new CreateChannel() { ChannelName = channelName, ChannelId = channelId });

            if (result is ErrorEventMessage)
            {
                throw new Exception("생성실패");
            }

            return testResult;
        }

        [HttpDelete]

        public async Task<string> DeleteChannel(string channelId)
        {
            string testResult = "";
            var result = await _channelManagerActor.Ask<object>(new DeleteChannel() { ChannelId = channelId });

            if (result is ErrorEventMessage)
            {
                throw new Exception("삭제실패");
            }

            return testResult;
        }

    }
}
