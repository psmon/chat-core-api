# ChatCore API ( SignalR with Akka.net)

웹소켓(Signalr)이 액터와 연동되어 채널관리가 되며, 최초 채널조인성공후 API를 통해서 커스텀한 푸시기능을 작성할수 있습니다. 

스탠드얼론 모드로, 라이트하게 다중채널 처리가되며 AKKA.net의 액터와 웹소켓세션이 연동되었기때문에 필요하면 클러스터로 확장할수 있습니다.

![SignalRWithActor](doc/SignalRWithActor.png)


## WebSocket TEST TOOL

![testtool](doc/testtool.png)

- 인증은 JWT토큰으로 간소화할수 있습니다. -무인증Mode
- 채널에 최초접속 이후에는 API를 이용하여 다양한 사용자에게 실시간 메시지를 보낼수 있습니다.
- 채널조인에 성공하면 ConnectID를 획득할수 있으며, 이 아이디는 특정사용자에게 메시지를 보낼때 활용되는 값입니다.


## WebAPI

![apis](doc/apis.png)

- ChannelManager : 채널을 생성하고 삭제할수 있습니다.
- Channel : 채널에 접속한 사용자에게 웹소켓 메시지를 보낼수 있습니다.
- ActorTest : 이벤트 자동분배(라운드로빈)등 다양한 기능을 시도해볼수 있습니다.
- 


# CodeReview

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


# 응용편
- https://getakka.net/articles/remoting/deployment.html : 로컬에서 작성한 액터는 리모트에도 큰 코드변경없이 배치가 가능하기때문에 리모트로 구성되어 다중처리가 가능합니다.
- https://getakka.net/articles/clustering/cluster-routing.html : 대용량 분산 처리가 필요할시 특정액터(채널액터)를 클러스터화 할수 있습니다.