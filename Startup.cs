using Autofac;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using Autofac.Integration.AspNetCore.Multitenant;

namespace WebApplication1
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
            services.AddMvc();
            services.AddControllers();

            services.AddScoped<ITest>(sp =>
            {
                return sp.GetRequiredService<Test>();
            });

            services.AddScoped<Test>();
            //services.AddScoped<ISampleDescription, SampleDescriptionGB>();
            services.AddAutofacMultitenantRequestServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
        }

        public static MultitenantContainer ConfigureMultitenantContainer(IContainer container)
        {
            var strategy = new MyTenantIdentificationStrategy(container.Resolve<IHttpContextAccessor>());
            var mtc = new MultitenantContainer(strategy, container);

            //configure tenant dependencies here
            mtc.ConfigureTenant(1, cb => cb.RegisterType<SampleDescriptionGB>().As<ISampleDescription>().InstancePerLifetimeScope());
            mtc.ConfigureTenant(2, cb => cb.RegisterType<SampleDescriptionDE>().As<ISampleDescription>().InstancePerLifetimeScope());

            return mtc;
        }

        public interface ITest
        {
            string GetId();
        }

        public class Test : ITest
        {
            private readonly string id;
            public Test()
            {
                id = Guid.NewGuid().ToString();
            }

            public string GetId()
            {
                return id;
            }
        }

        public class MyTenantIdentificationStrategy : ITenantIdentificationStrategy
        {
            private readonly IHttpContextAccessor httpContextAccessor;

            public MyTenantIdentificationStrategy(IHttpContextAccessor httpContextAccessor)
            {
                this.httpContextAccessor = httpContextAccessor;
            }

            public bool TryIdentifyTenant(out object tenantId)
            {
                tenantId = null;

                if (httpContextAccessor.HttpContext != null)
                {
                    var tenant = httpContextAccessor.HttpContext.Request.Headers["TenantId"].FirstOrDefault();

                    if (!string.IsNullOrEmpty(tenant))
                    {
                        tenantId = int.Parse(tenant);
                    }
                    else
                    {
                        tenantId = 1;
                    }
                }

                return tenantId != null;
            }
        }
    }

    public interface ISampleDescription { }
    public class SampleDescriptionGB : ISampleDescription { }
    public class SampleDescriptionDE : ISampleDescription { }
}
