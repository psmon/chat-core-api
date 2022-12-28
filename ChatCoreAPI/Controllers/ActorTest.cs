using ChatCoreAPI.Actors;

using Microsoft.AspNetCore.Mvc;

namespace ChatCoreAPI.Controllers
{
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
        public string Get(int testStep,string connectionId)
        {
            string testResult = "";

            switch (testStep)
            {
                case 0:
                {
                     _actorBridge.GetActorSystem().ActorOf(UserActor.Prop(connectionId, _serviceScopeFactory));
                };
                break;
            }

            return testResult;
        }
    }
}
