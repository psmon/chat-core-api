using Akka.Actor;

namespace ChatCoreAPI.Actors
{
    public interface IActorBridge
    {
        void Tell(object message);
        Task<T> Ask<T>(object message);

        ActorSystem GetActorSystem();
    }
}
