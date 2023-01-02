
using Akka.Actor;
using Akka.Event;
using Akka.TestKit;
using Akka.TestKit.Xunit;

using ChatCoreAPI.Actors;

namespace ChatCoreApiTest
{
    public class WebSocketMockActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        public WebSocketMockActor()
        {
            Receive<string>(message => {
                log.Info("Received String message: {0}", message);                
            });

            Receive<object>(message => {
                log.Info("Received object message: {0}", message);                
            });
        }
    }

    public class ChannelTest : TestKit
    {
        IActorRef userActor;

        TestProbe userActorTarget;

        IActorRef channelManagerActor;

        IActorRef websocket;

        TestProbe channelManagerActorTarget;


        public ChannelTest()
        {
            channelManagerActor = this.Sys.ActorOf(ChannelManagerActor.Prop(null));
            channelManagerActorTarget = this.CreateTestProbe();
            channelManagerActor.Tell(channelManagerActorTarget.Ref, this.TestActor);


            userActor = this.Sys.ActorOf(UserActor.Prop("test1", null));            
            websocket = this.Sys.ActorOf<WebSocketMockActor>();

            userActorTarget = this.CreateTestProbe();
            userActor.Tell(new ChatCoreAPI.Actors.TestActor() { actorRef = websocket, target = userActorTarget.Ref }, this.TestActor);
            ExpectMsg("done", TimeSpan.FromSeconds(1));

        }

        [Fact]
        public void JoinChannel()
        {
            channelManagerActor.Tell(new CreateChannel() { ChannelId="webnori", ChannelName="À¥³ë¸®" });
            channelManagerActorTarget.ExpectMsg("ok", TimeSpan.FromSeconds(1));

            userActor.Tell(new JoinChannel() { 
                ConnectionId = "test1",
                ChannelId = "webnori",
                ChannelManagerActor = channelManagerActor
            });

            userActorTarget.ExpectMsg<ChannelInfo>(TimeSpan.FromSeconds(1));

        }
    }
}