using ChatCoreAPI.Actors;

using Microsoft.AspNetCore.Mvc;

namespace ChatCoreAPI.Controllers
{
    public class ActorTest : ControllerBase
    {
        private IActorBridge _actorBridge;

        public ActorTest(IActorBridge actorBridge)
        {
            _actorBridge = actorBridge;
        }

        [HttpGet(Name = "ActorTest")]
        public string Get(int testStep,string connectionId)
        {
            string testResult = "";

            switch (testStep)
            {
                case 0:
                {
                     _actorBridge.GetActorSystem().ActorOf(UserActor.Prop(connectionId));
                };
                break;
            }

            return testResult;
        }
    }
}
