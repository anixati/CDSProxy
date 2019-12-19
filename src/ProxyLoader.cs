using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrmWebApiProxy
{
    public class ProxyLoader
    {
        public ProxyLoader(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<ProxyConfig>(Configuration.GetSection("ProxyConfig"));
            services.AddSingleton<ICRMAuthenticator, CRMProxyAuthenticator>(sp =>
            {
                using (var scope = sp.CreateScope())
                {
                    return CRMProxyAuthenticator.Create(scope.ServiceProvider.GetService<IOptions<ProxyConfig>>());
                }
            });

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    );
            });

            //services.AddCors(co=> {
            //   co.AddPolicy("AllowAllHeaders", b => { b.AllowAnyOrigin().AllowAnyHeader(); });
            //   });
            services.AddRouting(options => { options.LowercaseUrls = true; });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            //  app.UseHttpsRedirection();
            app.UseCors("CorsPolicy");
            // app.UseCors(builder =>              builder.AllowAnyOrigin());

            app.UseMiddleware<ProxyMiddleware>();
            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("If you are see this means you are not using this as proxy server");
            });
        }
    }
}