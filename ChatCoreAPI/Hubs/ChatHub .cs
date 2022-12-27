﻿using Akka.Actor;

using ChatCoreAPI.Actors;

using Microsoft.AspNetCore.SignalR;

namespace ChatCoreAPI.Hubs
{
    public class ChatHub : Hub
    {
        private IActorBridge _actorBridge { get; set; }

        private IActorRef _userActor { get; set; }

        public ChatHub(IActorBridge actorBridge)
        {
            _actorBridge = actorBridge;
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task JoinChannel(string channel, string id, string pw) 
        {

        }

        public override async Task OnConnectedAsync()
        {            
            _userActor = _actorBridge.GetActorSystem().ActorOf(UserActor.Prop(Context.ConnectionId));

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _userActor.Tell(PoisonPill.Instance);

            await base.OnDisconnectedAsync(exception);
        }


    }
}
