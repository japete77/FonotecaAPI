using Autofac;
using Config.Implementations;
using Config.Interfaces;

namespace Belsize.Configuration.AutofacModules
{
    public class ServicesModule : Module
    {
        public static void Register(ContainerBuilder builder)
        {
            builder.RegisterType<Settings>().As<ISettings>().InstancePerLifetimeScope();
        }
    }
}
