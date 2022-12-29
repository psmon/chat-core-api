using Akka.Actor;

namespace ChatCoreAPI.Actors
{
    public interface IActorBridge
    {
        void Tell(object message);
        Task<T> Ask<T>(object message);

        IActorRef GetActorManager();

        IActorRef GetChannelActor(string channelId);

        ActorSystem GetActorSystem();
    }
}
