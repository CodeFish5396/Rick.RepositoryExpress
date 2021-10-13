using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Rick.RepositoryExpress.Common;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Service;
using Rick.RepositoryExpress.WebApi.Filters;
using Rick.RepositoryExpress.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.WebApi
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
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Demo", Version = "v1" });
                c.OperationFilter<AddAuthTokenHeaderParameter>();
                var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);//获取应用程序所在目录（绝对，不受工作目录影响，建议采用此方法获取路径）
                var xmlPath = Path.Combine(basePath, "SwaggerDemo.xml");
                c.IncludeXmlComments(xmlPath);
            });
            var connectionString = Configuration.GetConnectionString("Database");
            var version = Configuration.GetConnectionString("Version");
            services.AddDbContext<RickDBConext>(options => options.UseMySql(connectionString, ServerVersion.Parse(version)));
            services.AddScoped<IAppuserService, AppuserService>();
            services.AddScoped<IFileService, FileService>();

            services.AddScoped<IRepositoryService, RepositoryService>();
            services.AddScoped<IExpressclaimService, ExpressclaimService>();

            services.AddSingleton<IIdGeneratorService, SnowFlakeService>();
            var redisConnectionString = Configuration.GetConnectionString("RedisConnection");
            var redisDbNum = Convert.ToInt32(Configuration.GetConnectionString("RedisDbNum"));
            services.AddSingleton(new RedisClientService(redisConnectionString, redisDbNum));
            services.AddControllersWithViews(configure =>
            {
                configure.Filters.Add<CustomExceptionFilterAttribute>();
                configure.Filters.Add<CustomAuthorizationFilterAttribute>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Demo v1");
            });
            app.UseStaticFiles();

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
