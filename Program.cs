using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.Json;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace NuevaLuz
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")))
            {
                CreateWebHostBuilder(args).Build().Run();
            }
            else
            {
                var lambdaEntry = new LambdaEntryPoint();
                var functionHandler = (Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>>)(lambdaEntry.FunctionHandlerAsync);
                using var handlerWrapper = HandlerWrapper.GetHandlerWrapper(functionHandler, new JsonSerializer());
                using var bootstrap = new LambdaBootstrap(handlerWrapper);
                bootstrap.RunAsync().Wait();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddAutofac())
                .ConfigureAppConfiguration(x =>
                {
                    x.AddEnvironmentVariables();
                })
                .UseStartup<Startup>();

    }
}
