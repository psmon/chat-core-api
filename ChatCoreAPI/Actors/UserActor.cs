using Akka.Actor;
using Akka.Event;

using ChatCoreAPI.Actors.Models;
using ChatCoreAPI.Hubs;

using Microsoft.AspNetCore.SignalR;

namespace ChatCoreAPI.Actors
{
    public class UserActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private IActorRef _channelActor { get; set; }

        private readonly IServiceScopeFactory _scopeFactory;

        private string ConnectionId { get; set; }

        private string ChannelId { get; set; }

        public bool IsJoinGroup { get; set; }

        public UserActor(string connectionId, IServiceScopeFactory scopeFactory)
        {
            log.Info("Create UserActor: {0}", connectionId);

            _scopeFactory = scopeFactory;

            ConnectionId = connectionId;

            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });            

            ReceiveAsync<SendAllGroup>(async message => {
                log.Info("Received String message: {0}", message);

                if (!string.IsNullOrEmpty(message.SubGroup))
                {
                    await SendToSubGroup(message.SubGroup, new WSSendEvent()
                    {
                        EventType = message.EventType,
                        ChannelId = message.ChannelId,
                        ChannelName = message.ChannelName,
                        EventData = message.EventData
                    });                
                }
                else
                {
                    await SendToSelfConnection(new WSSendEvent()
                    {
                        EventType = message.EventType,
                        ChannelId = message.ChannelId,
                        ChannelName = message.ChannelName,
                        EventData = message.EventData
                    });
                }
            });

            ReceiveAsync<SendSomeOne>(async message => {
                log.Info("Received String message: {0}", message);
                await SendToConnectionId(message.ConnectId, new WSSendEvent()
                {
                    EventType = message.EventType,
                    ChannelId = message.ChannelId,
                    ChannelName = message.ChannelName,
                    EventData = message.EventData
                });
            });

            Receive<string>(message => {
                log.Info("Received String message: {0}", message);
                Sender.Tell(message);
            });


            ReceiveAsync<AutoAssignInfo>(async message => {
                log.Info("Received AutoAssign message: {0}", message.AsignData);
                await SendToSelfConnection(new WSSendEvent() { 
                    EventType = "AutoAssign",
                    ChannelId = ChannelId,
                    ChannelName = "",
                    EventData = message.AsignData
                });
            });

            ReceiveAsync<JoinGroup>(async message => {
                if (!IsJoinGroup)
                {
                    Self.Tell(new ErrorEventMessage()
                    {
                        ErrorCode = -1,
                        ErrorMessage = "가입된 채널이 없습니다."
                    });
                    return;
                };

                log.Info("Received JoinGroup message: {0}", message);
                await OnJoinGroup(message);
            });

            Receive<JoinChannel>(message => {
                log.Info("Received JoinChannel message: {0}", message);

                if (IsJoinGroup)
                {
                    Self.Tell(new ErrorEventMessage()
                    {
                        ErrorCode = -1,
                        ErrorMessage = "하나의 채널에만 가입가능합니다. 채널을 변경하려면 LeaveChannel 을 이용해주세요"
                    });
                    return;
                }                    

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


            //LeaveChannel
            Receive<LeaveChannel>(message => {
                log.Info("Received LeaveChannel message: {0}", message);

                if (!IsJoinGroup)
                {
                    Self.Tell(new ErrorEventMessage()
                    {
                        ErrorCode = -1,
                        ErrorMessage = "가입된 채널이 없습니다."
                    });
                    return;
                };

                var result = message.ChannelManagerActor.Ask(new ChannelInfo()
                {
                    ChannelId = message.ChannelId
                }).Result;

                if (result is ChannelInfo)
                {
                    ChannelInfo channelInfo = result as ChannelInfo;
                    _channelActor = channelInfo.ChannelActor;

                    _channelActor.Tell(message);
                }
                else if (result is ErrorEventMessage)
                {
                    Self.Tell(result);
                }
            });


            Receive<ChannelInfo>(async message => {
                log.Info("Received ChannelInfo message: {0}", message);
                await OnJoinChannel(message);
            });

            Receive<ErrorEventMessage>(async message => {
                log.Error($"Received ErrorEvent message: {message.ErrorCode} {message.ErrorMessage}");
                await OnErrorMessage(message);
            });


        }

        public async Task OnJoinChannel(ChannelInfo channelInfo)
        {
            await SendToSelfConnection(new WSSendEvent()
            {
                EventType = "OnJoinChannel",
                ChannelId = channelInfo.ChannelId,
                ChannelName = channelInfo.ChannelName,
                EventData = $"connectId:{ConnectionId}"
            });

            ChannelId = channelInfo.ChannelId;
            IsJoinGroup = true;

            await JoinGroup(channelInfo.ChannelId);

            await SendToGroup(new WSSendEvent()
            {
                EventType = "OnJoinChannel-GroupNoti",
                ChannelId = channelInfo.ChannelId,
                ChannelName = channelInfo.ChannelName,
                EventData = String.Empty
            });
        }

        public async Task OnJoinGroup(JoinGroup joinGroup)
        {            
            await JoinGroup($"{joinGroup.ChannelId}-{joinGroup.SubGorup}");

            await SendToSelfConnection(new WSSendEvent()
            {
                EventType = "OnJoinGroup",
                ChannelId = joinGroup.ChannelId,
                EventData = joinGroup.SubGorup,
                ChannelName = ""
            });

        }

        //JoinGroup


        public async Task SendToSelfConnection(WSSendEvent wSSendEvent)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                await wsHub.Clients.Client(ConnectionId).SendAsync("ReceiveMessage",
                    wSSendEvent.EventType, wSSendEvent.ChannelId, wSSendEvent.ChannelName, wSSendEvent.EventData );
            }
        }

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

        public async Task SendToSubGroup(string subGroup, WSSendEvent wSSendEvent)
        {
            if (!IsJoinGroup)
                return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                await wsHub.Clients.Group(ChannelId +"-"+ subGroup).SendAsync("ReceiveMessage",
                    wSSendEvent.EventType, wSSendEvent.ChannelId, wSSendEvent.ChannelName, wSSendEvent.EventData);
            }
        }

        public async Task JoinGroup(string groupName)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Groups.AddToGroupAsync(ConnectionId, groupName);
            }
        }

        public async Task OnErrorMessage(ErrorEventMessage errorEvent)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Clients.Client(ConnectionId).SendAsync("ErrorMessage", errorEvent.ErrorCode, errorEvent.ErrorMessage);
            }
        }


        public static Props Prop(string connectionId, IServiceScopeFactory scopeFactory)
        {
            return Akka.Actor.Props.Create(() => new UserActor(connectionId, scopeFactory));
        }

        protected override void PostStop()
        {
            if (IsJoinGroup)
            {
                _channelActor.Tell(new LeaveChannel() { ChannelId = ChannelId, ConnectionId = ConnectionId });
            }

            log.Info("Stop UserActor: {0}", ConnectionId);
            base.PostStop();
        }
    }
}
