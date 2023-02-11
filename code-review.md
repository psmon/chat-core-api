
# CodeReview

## Unit Test


메시징처리에대한 유닛테스트를 지원합니다.

```
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
            channelManagerActor.Tell(new CreateChannel() { ChannelId="webnori", ChannelName="웹노리" });
            channelManagerActorTarget.ExpectMsg("ok", TimeSpan.FromSeconds(1));

            userActor.Tell(new JoinChannel() { 
                ConnectionId = "test1",
                ChannelId = "webnori",
                ChannelManagerActor = channelManagerActor
            });

            userActorTarget.ExpectMsg<ChannelInfo>(TimeSpan.FromSeconds(1));
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

            //99개의 일을 특정채널에 라운드로빈 배분
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
```

## 커넥션 세션 관리

```
public class ChatHub : Hub
.......................   
        public override async Task OnConnectedAsync()
        {            
            _actorBridge.GetActorSystem().ActorOf(UserActor.Prop(Context.ConnectionId, _serviceScopeFactory), Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                IActorRef _userActor = await GetUserActor();
                _userActor.Tell(PoisonPill.Instance);
            }
            finally
            {
                Console.WriteLine($"OnDisconnectedAsync {Context.ConnectionId}");
                await base.OnDisconnectedAsync(exception);
            }
        }
```
- 클라이언트 관리를 위해 UserActor가 웹소켓 커넥션 ID를 관리하게 됩니다.

---
## Front -> Server 전송부

```
wwwroor/chat.js
.......................
    document.getElementById("send").addEventListener("click", async () => {
        const actionInput = document.getElementById("actionInput").value;
        const input1 = document.getElementById("input1").value;
        const input2 = document.getElementById("input2").value;
        const input3 = document.getElementById("input3").value;

        // <snippet_Invoke>
        try {
            await connection.invoke(actionInput, input1, input2, input3);
        } catch (err) {
            console.error(err);
        }
        // </snippet_Invoke>
    });
```

```
public class ChatHub : Hub
.......................    
        public async Task JoinChannel(string channelId, string loginId, string accessToken) 
        {
            JoinChannel joinChannel = new JoinChannel()
            {
                ConnectionId = Context.ConnectionId,
                ChannelId = channelId,
                AccessToken = accessToken,
                LoginId = loginId,
                ChannelManagerActor = _actorBridge.GetActorManager()                
            };

            IActorRef _userActor = await GetUserActor();
            _userActor.Tell(joinChannel);
        }
```

- 전송시 입력된 actionInput부분이 ChatHub의 함수에 대응하여 수신이되며~ 유사한 패턴으로 프론트->서버 이벤트기능이 필요할시 추가될수 있습니다.
- 전송 데이터 파라미터는 개수에 맞춰서 자유롭게 추가가 가능합니다.
---


## Server -> Client
```
    public UserActor(string connectionId, IServiceScopeFactory scopeFactory)
    {
        log.Info("Create UserActor: {0}", connectionId);

        _scopeFactory = scopeFactory;

        ConnectionId = connectionId;
        
        Receive<JoinChannel>(message => {
            log.Info("Received String message: {0}", message);

            var result = message.ChannelManagerActor.Ask(new ChannelInfo() { 
                ChannelId = message.ChannelId
            }).Result;

            if (result is ChannelInfo)
            {
                ChannelInfo channelInfo = result as ChannelInfo;
                _channelActor = channelInfo.ChannelActor;

                _channelActor.Tell(message);
            } 
            else if (result is ErrorEventMessage) {
                Self.Tell(result);
            }
        });

        public async Task SendToConnectionId(string connectionId, WSSendEvent wSSendEvent)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                await wsHub.Clients.Client(connectionId).SendAsync("ReceiveMessage",
                    wSSendEvent.EventType, wSSendEvent.ChannelId, wSSendEvent.ChannelName, wSSendEvent.EventData);
            }
        }

        public async Task SendToGroup(WSSendEvent wSSendEvent)
        {
            if (!IsJoinGroup)
                return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                await wsHub.Clients.Group(ChannelId).SendAsync("ReceiveMessage",
                    wSSendEvent.EventType, wSSendEvent.ChannelId, wSSendEvent.ChannelName, wSSendEvent.EventData);
            }
        }
```
- 클라이언트에게 다양한 방법으로 푸시전송(ConnectID,ChannelID,BroadCast)이 가능합니다.
- ChannelActor는 ChannelID를 관리합니다.

## 전송API

```
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
```
- 푸시처리를 위해 웹소켓을 항상이용할 필요없이 RESTAPI를 활용하여 Server-Client(web) 푸시메시지 전송이 가능합니다.

