using Akka.Actor;

using ChatCoreAPI.Actors;
using ChatCoreAPI.Actors.Models;

using Microsoft.AspNetCore.SignalR;

namespace ChatCoreAPI.Hubs
{    
    public class ChatHub : Hub
    {
        private IActorBridge _actorBridge { get; set; }

        private IServiceScopeFactory _serviceScopeFactory;

        //private IActorRef _userActor { get; set; }

        public ChatHub(IActorBridge actorBridge, IServiceScopeFactory serviceScopeFactory)
        {
            _actorBridge = actorBridge;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task JoinChannel(string channelId, string loginId, string accessToken) 
        {
            JoinChannel joinChannel = new JoinChannel()
            {
                ChannelId = channelId,
                AccessToken = accessToken,
                LoginId = loginId,
                ChannelManagerActor = _actorBridge.GetActorManager()                
            };

            IActorRef _userActor = await GetUserActor();
            _userActor.Tell(joinChannel);
        }

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

        protected async Task<IActorRef> GetUserActor()
        {
            return await _actorBridge.GetActorSystem().ActorSelection($"user/{Context.ConnectionId}")
                .ResolveOne(TimeSpan.FromSeconds(3));
        }


    }
}
