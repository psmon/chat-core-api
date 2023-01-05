using System.Runtime.CompilerServices;

using Akka.Actor;

using ChatCoreAPI.Actors;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json.Linq;

namespace ChatCoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ActorTest : ControllerBase
    {
        private IActorBridge _actorBridge;

        private IServiceScopeFactory _serviceScopeFactory;

        public ActorTest(IActorBridge actorBridge, IServiceScopeFactory serviceScopeFactory)
        {
            _actorBridge = actorBridge;

            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpPut("AutoAssign")]        
        public async Task<string> AutoAssign(string channelId)
        {
            string testResult = "OK";
            _actorBridge.GetChannelActor(channelId).Tell("AutoAssignTest");
            return testResult;
        }

        [HttpPost("CreateUserActor")] 
        public async Task<string> CreateUserActor(string connectionId)
        {
            string testResult = "";
            _actorBridge.GetActorSystem().ActorOf(UserActor.Prop(connectionId, _serviceScopeFactory));
            return testResult;
        }

        [HttpPost("LineHook")]
        public async Task<string> HookLine(dynamic param)
        {
            //dynamic data = JObject.Parse(param);

            

            return "OK";
        }

    }
}
