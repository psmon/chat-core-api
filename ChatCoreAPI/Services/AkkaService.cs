using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Dispatch.SysMsg;

using ChatCoreAPI.Actors;

namespace ChatCoreAPI.Services
{
    public class AkkaService : IHostedService, IActorBridge
    {
        private ActorSystem _actorSystem;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory _scopeFactory;

        
        private IActorRef _actorRef;

        private readonly IHostApplicationLifetime _applicationLifetime;

        public AkkaService(IServiceScopeFactory scopeFactory, IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = appLifetime;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var bootstrap = BootstrapSetup.Create();

            // enable DI support inside this ActorSystem, if needed
            var diSetup = DependencyResolverSetup.Create(_serviceProvider);

            // merge this setup (and any others) together into ActorSystemSetup
            var actorSystemSetup = bootstrap.And(diSetup);

            // start ActorSystem
            _actorSystem = ActorSystem.Create("akka-universe", actorSystemSetup);

            _actorRef = _actorSystem.ActorOf(ChannelManagerActor.Prop(_scopeFactory), "ChannelManagerActor");

            ActorTest();

            // add a continuation task that will guarantee shutdown of application if ActorSystem terminates
            //await _actorSystem.WhenTerminated.ContinueWith(tr => {
            //   _applicationLifetime.StopApplication();
            //});
            _actorSystem.WhenTerminated.ContinueWith(tr => {
                _applicationLifetime.StopApplication();
            });
            await Task.CompletedTask;
        }

        public void ActorTest()
        {
            _actorRef.Tell(new CreateChannel() { ChannelName = "웹노리", ChannelId = "webnori" });

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // strictly speaking this may not be necessary - terminating the ActorSystem would also work
            // but this call guarantees that the shutdown of the cluster is graceful regardless
            await CoordinatedShutdown.Get(_actorSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
        }

        public void Tell(object message)
        {
            _actorRef.Tell(message);
        }

        public async Task<T> Ask<T>(object message)
        {
            return await _actorRef.Ask<T>(message);
        }

        public ActorSystem GetActorSystem()
        {
            return _actorSystem;
        }

        public IActorRef GetActorManager()
        {
            return _actorRef;
        }

        public IActorRef GetChannelActor(string channelId)
        {
            var result = _actorRef.Ask(new ChannelInfo()
            {
                ChannelId = channelId
            }).Result;

            if (result is ChannelInfo)
            {
                ChannelInfo channelInfo = result as ChannelInfo;
                var _channelActor = channelInfo.ChannelActor;
                return _channelActor;
            }

            return null;
        }
    }
}
