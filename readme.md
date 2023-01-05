# ChatCore API ( SignalR with Akka.net)

웹소켓(Signalr)이 액터와 연동되어 세션단위 채널관리가 되며 API를 통해 채널 푸시기능을 지원합니다.
도메인에대한 기능은 가지고 있지않으며 여기서 제공하는 채널기능을 이용하여
다음과 같은 다양한 서비스를 커스텀하게 구현할수 있습니다.

- 다중채널지원 채팅솔루션
- 다중채널지원 푸시 시스템
- 채널내 라운드로빈 처리기능(채팅상담 분배)

![SignalRWithActor](doc/SignalRWithActor.png)


## WebSocket TEST TOOL

![testtool](doc/testtool.png)

- 채널에 최초접속 이후에는 API를 이용하여 다양한 사용자에게 실시간 메시지를 보낼수 있습니다.
- 채널조인에 성공하면 ConnectID를 획득할수 있으며, 이 아이디는 특정사용자에게 메시지를 보낼때 활용되는 값입니다.


## WebAPI

![apis](doc/apis.png)

- ChannelManager : 채널을 생성하고 삭제할수 있습니다.
- Channel : 채널에 접속한 사용자에게 웹소켓 메시지를 보낼수 있습니다.
- ActorTest : 이벤트 자동분배(라운드로빈)등 다양한 기능을 시도해볼수 있습니다.
- 


## 테스트 시나리오

### 서버 채널 생성

```
curl -X 'POST' \
  'http://localhost:9595/ChannelManager?channelId=test1&channelName=%ED%85%8C%EC%8A%A4%ED%8A%B81' \
  -H 'accept: text/plain' \
  -d ''
```

### WS 채널/그룹 가입

#### 채널가입

```
JoinChannel(test1,testtoken,testdata)

Result:
OnRecvMessage OnJoinChannel : test1 테스트1 connectId:gfcn_xBKSqLlHoDzrIKABg
```

#### 채널내 그룹가입
```
JoinGroup(test1,testtoken,group1)
```



### 메시지 발송

#### 특정채널

```
curl -X 'POST' \
  'http://localhost:9595/Channel/SendToChannelAllUser' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "eventType": "test",
  "channelId": "test1",
  "channelName": "테스트1",
  "eventData": "test data",
  "subGroup": ""
}'
```

#### 특정그룹

```
curl -X 'POST' \
  'http://localhost:9595/Channel/SendToChannelAllUser' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "eventType": "test",
  "channelId": "test1",
  "channelName": "테스트1",
  "eventData": "test data",
  "subGroup": "testgroup1"
}'
```

#### 특정채널에 라운로로빈 메시지
```
curl -X 'POST' \
  'http://localhost:9595/Channel/AutoAsign' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "channelId": "webnori",
  "asignData": "metadata"
}'
```

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
```


## CodeReview

[코드리뷰](code-review.md)