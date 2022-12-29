using ChatCoreAPI.Actors;
using ChatCoreAPI.Actors.Models;
using Akka.Actor;

using ChatCoreAPI.Actors;
using Microsoft.AspNetCore.Mvc;

namespace ChatCoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChannelController
    {
        private IActorBridge _actorBridge;

        private IServiceScopeFactory _serviceScopeFactory;

        public ChannelController(IActorBridge actorBridge, IServiceScopeFactory serviceScopeFactory)
        {
            _actorBridge = actorBridge;

            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpPost("SendToChannelAllUser")]
        public async Task<string> SendToChannelAllUser(SendAllGroup sendData)
        {
            string testResult = "";
            _actorBridge.GetChannelActor(sendData.ChannelId).Tell(sendData);
            return testResult;
        }

        [HttpPost("SendToChannelSomeOne")]
        public async Task<string> SendToChannelSomeOne(SendSomeOne sendData)
        {
            string testResult = "";
            _actorBridge.GetChannelActor(sendData.ChannelId).Tell(sendData);
            return testResult;
        }

    }
}
