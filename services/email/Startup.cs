﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.Util;
using Microservices.Services.Core.Consumers;
using Microservices.Services.Core.Interface.Services;
using Microservices.Services.Core.Providers;
using Microservices.Services.Core.Repositories;
using Microservices.Services.Core.Services;
using Microservices.Services.Infrastructure.Providers;
using Microservices.Services.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace email
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDbProvider, DbProvider>();
            services.AddTransient<IEmailRepository, EmailRepository>();
            services.AddTransient<IEmailService, EmailService>();

            services.AddScoped<CreateUserConsumer>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddMassTransit(x => {
                x.AddConsumer<CreateUserConsumer>();

                x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg => {
                    
                    var host = cfg.Host(new Uri($"rabbitmq://{Configuration["RabbitMQHostName"]}"), hostConfig => {
                        hostConfig.Username("guest");
                        hostConfig.Password("guest");
                    });

                    cfg.ReceiveEndpoint(host, "create_user", ep => {
                        ep.PrefetchCount = 16;
                        ep.UseMessageRetry(mr => mr.Interval(2, 100));

                        ep.ConfigureConsumer<CreateUserConsumer>(provider);
                    });
                }));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseMvc();

            var bus = app.ApplicationServices.GetService<IBusControl>();
            var busHandle = TaskUtil.Await(() =>
            {
                return bus.StartAsync();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                busHandle.Stop();
            });
        }
    }
}
