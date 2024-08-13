using Autofac;
using Autofac.Configuration;
using Belsize.Modules;
using Config.Implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NuevaLuz.Fonoteca.Filters;
using NuevaLuz.Fonoteca.Middleware;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

/*
 * This project have been originally created from ASP.Net Core RESTful Service Template.
 * Getting started guide: https://github.com/drwatson1/AspNet-Core-REST-Service/wiki/Getting-Started-Guide
 * More information about configuring project: https://github.com/drwatson1/AspNet-Core-REST-Service/wiki
 */

namespace NuevaLuz
{
    public class Startup
    {
        ILogger<Startup> _logger { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            Startup.Configuration = configuration;

            // Get app version
            var assembly = Assembly.GetExecutingAssembly();
            Startup.AppVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;

            _logger = logger;

            // https://github.com/drwatson1/AspNet-Core-REST-Service/wiki#using-environment-variables-in-configuration-options
            var envPath = Path.Combine(env.ContentRootPath, ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load();
            }

            // See: https://github.com/drwatson1/AspNet-Core-REST-Service/wiki#content-formatting
            JsonConvert.DefaultSettings = () =>
                new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Include,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
#if DEBUG
                    Formatting = Formatting.Indented
#else
                    Formatting = Formatting.None
#endif
                };
        }

        public static IConfiguration Configuration { get; private set; }
        public static string AppVersion { get; set; }
        private static IApplicationBuilder Application { get; set; }
        public static T GetService<T>()
        {
            if (Startup.Application != null)
            {
                return (T)Startup.Application.ApplicationServices.GetService(typeof(T));
            }

            return default;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Fonoteca API", Version = "v1" });
            });

            services.AddCors();

            // Add useful interface for accessing the ActionContext outside a controller.
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            // Add useful interface for accessing the HttpContext outside a controller.
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add useful interface for accessing the IUrlHelper outside a controller.
            services.AddScoped<IUrlHelper>(x => x
                .GetRequiredService<IUrlHelperFactory>()
                .GetUrlHelper(x.GetRequiredService<IActionContextAccessor>().ActionContext));

            services.AddControllers(options =>
            {
                options.Filters.Add(new CacheControlFilter());
                options.Filters.Add(new SecurityFilter(new HttpContextAccessor()));
                options.EnableEndpointRouting = false; // Disable endpoint routing if using traditional MVC
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver { NamingStrategy = null };
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;                
#if DEBUG
                options.SerializerSettings.Formatting = Formatting.Indented;
#else
        options.SerializerSettings.Formatting = Formatting.None;
#endif
            });

            services.AddEndpointsApiExplorer(); // This replaces AddApiExplorer in .NET 6.0

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(Constants.App.ResetPasswordTokenExpirationInMinutes);
            });
        }

        /// <summary>
        /// Configure Autofac DI-container
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <remarks>
        /// ConfigureContainer is where you can register things directly
        /// with Autofac. This runs after ConfigureServices so the things
        /// here will override registrations made in ConfigureServices.
        /// Don't build the container; that gets done for you.
        /// 
        /// See: https://github.com/drwatson1/AspNet-Core-REST-Service/wiki#dependency-injection
        /// </remarks>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Add things to the Autofac ContainerBuilder.
            builder.RegisterModule<DefaultModule>();
            builder.RegisterModule(new ConfigurationModule(Configuration));
        }

        /// <summary>
        /// Configure Autofac DI-container for production
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <remarks>
        /// This only gets called if your environment is Production. The
        /// default ConfigureContainer won't be automatically called if this
        /// one is called.
        /// 
        /// See: https://github.com/drwatson1/AspNet-Core-REST-Service/wiki#dependency-injection
        /// </remarks>
        public void ConfigureProductionContainer(ContainerBuilder builder)
        {
            ConfigureContainer(builder);

            // Add things to the ContainerBuilder that are only for the
            // production environment.
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            var settings = new Settings(Startup.Configuration);

            Startup.Application = app;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Use an exception handler middleware before any other handlers
            app.UseFonotecaExceptionHandler();

            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseRouting();

            app.UseAuthorization();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve Swagger-UI (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fonoteca API V1");
                c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            logger.LogInformation("Server started");
        }
    }
}
