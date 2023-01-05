
using Akka.Actor;
using Akka.Event;
using Akka.TestKit;

using ChatCoreAPI.Actors;

using Xunit.Abstractions;

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

    public class ChannelTest : TestKitXunit
    {
        IActorRef userActor;

        IActorRef channelActor;

        IActorRef channelManagerActor;

        IActorRef websocket;

        TestProbe userActorProbe;

        TestProbe channelManagerActorProbe;

        public ChannelTest(ITestOutputHelper output) : base(output)
        {
            Setup();
        }

        public void Setup()
        {
            userActor = this.Sys.ActorOf(UserActor.Prop("test1", null));
            websocket = this.Sys.ActorOf<WebSocketMockActor>();

            userActorProbe = this.CreateTestProbe();
            channelManagerActorProbe = this.CreateTestProbe();


            channelManagerActor = this.Sys.ActorOf(ChannelManagerActor.Prop(null));
            
            channelManagerActor.Tell(new TestActorInfo() { targetActor = channelManagerActorProbe.Ref }, this.TestActor);
            ExpectMsg("done", TimeSpan.FromSeconds(1));
            
            userActor.Tell(new TestActorInfo() { mockActor = websocket, targetActor = userActorProbe.Ref }, this.TestActor);
            ExpectMsg("done", TimeSpan.FromSeconds(1));

            channelManagerActor.Tell(new CreateChannel() { ChannelId = "webnori", ChannelName = "웹노리" });
            channelManagerActorProbe.ExpectMsg("ok-CreateChannel", TimeSpan.FromSeconds(1));

            var result = channelManagerActor.Ask(new ChannelInfo()
            {
                ChannelId = "webnori"
            }).Result;

            if (result is ChannelInfo)
            {
                channelActor = (result as ChannelInfo).ChannelActor;
            }

        }


        [Fact]
        public void JoinChannel()
        {            

            userActor.Tell(new JoinChannel() { 
                ConnectionId = "test1",
                ChannelId = "webnori",
                ChannelManagerActor = channelManagerActor
            });
            userActorProbe.ExpectMsg<ChannelInfo>(TimeSpan.FromSeconds(1));


            userActor.Tell(new JoinChannel()
            {
                ConnectionId = "test1",
                ChannelId = "webnori",
                ChannelManagerActor = channelManagerActor
            });
            userActorProbe.ExpectMsg<ErrorEventMessage>(TimeSpan.FromSeconds(1));

            userActor.Tell(new LeaveChannel()
            {
                ConnectionId = "test1",
                ChannelId = "webnori",
                ChannelManagerActor = channelManagerActor
            });
            userActorProbe.ExpectMsg("Ok-LeaveChannel", TimeSpan.FromSeconds(1));            

        }

        [Fact]
        public void RoundRobinTest() 
        {            

            TestProbe[] testProbes = new []{ this.CreateTestProbe(), this.CreateTestProbe(), this.CreateTestProbe() };

            //Given : 3명의 상담원생성
            for (int i = 0; i < 3; i++)
            {
                string userId = "test" + i;
                var _userActor = this.Sys.ActorOf(UserActor.Prop(userId, null));
                _userActor.Tell(new TestActorInfo() { mockActor = websocket, targetActor = testProbes[i].Ref }, this.TestActor);


                //When : 채널에 조인
                _userActor.Tell(new JoinChannel()
                {
                    ConnectionId = userId,
                    ChannelId = "webnori",
                    ChannelManagerActor = channelManagerActor
                });                
            }

            //채널에 가입완료 체크
            for (int i = 0; i < 3; i++)
            {
                testProbes[i].ExpectMsg<ChannelInfo>(TimeSpan.FromSeconds(1));
            }

            //99개의 일을 배분
            for (int i = 0; i < 99; i++)
            {
                string taskName = "i" + i;
                channelActor.Tell(new AutoAsign() { AsignData=taskName,ChannelId="webnori" });
            }

            //작업균등 분배되었는지 확인
            for (int i = 0; i < 33; i++) 
            {
                testProbes[0].ExpectMsg<AutoAssignInfo>(TimeSpan.FromSeconds(1));
                testProbes[1].ExpectMsg<AutoAssignInfo>(TimeSpan.FromSeconds(1));
                testProbes[2].ExpectMsg<AutoAssignInfo>(TimeSpan.FromSeconds(1));
            }

        }


    }
}