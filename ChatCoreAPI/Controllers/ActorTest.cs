using Akka.Actor;

using ChatCoreAPI.Actors;

using Microsoft.AspNetCore.Mvc;

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

        [HttpGet(Name = "ActorTest")]
        public async Task<string> Get(int testStep,string connectionId)
        {
            string testResult = "";

            switch (testStep)
            {
                case 0:
                     _actorBridge.GetActorSystem().ActorOf(UserActor.Prop(connectionId, _serviceScopeFactory));
                break;
                case 1:
                    _actorBridge.GetChannelActor("webnori").Tell("AutoAssignTest");
                break;                
            }

            return testResult;
        }
    }
}
