using Autofac;
using Config.Implementations;
using Config.Interfaces;
using NuevaLuz.Fonoteca.Services.Fonoteca.Implementations;
using NuevaLuz.Fonoteca.Services.Fonoteca.Interfaces;

namespace Belsize.Configuration.AutofacModules
{
    public class ServicesModule : Module
    {
        public static void Register(ContainerBuilder builder)
        {
            builder.RegisterType<Settings>().As<ISettings>().InstancePerLifetimeScope();
            builder.RegisterType<FonotecaService>().As<IFonotecaService>().InstancePerLifetimeScope();
        }
    }
}
