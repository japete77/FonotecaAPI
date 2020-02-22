using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using NuevaLuz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuevaLuz.Fonoteca.Middleware
{
    public static class SwaggerMiddlewareExtentions
    {
        /// <summary>
        /// Add Swagger middleware
        /// </summary>
        /// <remarks>
        /// See: https://github.com/drwatson1/AspNet-Core-REST-Service/wiki#documenting-api
        /// </remarks>
        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(Constants.Swagger.Version,
                    new OpenApiInfo { Title = Constants.Swagger.ApiName, Version = Constants.Swagger.Version });

                // Set the comments path for the Swagger JSON and UI.
                List<string> xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly).ToList();
                xmlFiles.ForEach(xmlFile => c.IncludeXmlComments(xmlFile));

            });

            return services;
        }

        /// <summary>
        /// Use swagger UI and endpoint
        /// </summary>
        /// <remarks>
        /// See: https://github.com/drwatson1/AspNet-Core-REST-Service/wiki#documenting-api
        /// </remarks>
        public static IApplicationBuilder UseSwaggerWithOptions(this IApplicationBuilder app)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            SwaggerBuilderExtensions.UseSwagger(app);

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                // Uncomment this line if you want to access the Swagger UI as http://localhost:5000
                // c.RoutePrefix = string.Empty;
                c.SwaggerEndpoint(Constants.Swagger.EndPoint, Constants.Swagger.ApiName);
            });

            return app;
        }
    }
}
