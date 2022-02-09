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
using Rick.RepositoryExpress.SysWebApi.Models;
using Rick.RepositoryExpress.SysWebApi.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Net.WebSockets;
using System.Net;

namespace Rick.RepositoryExpress.SysWebApi
{
    public class Startup
    {
        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                  builder =>
                                  {
                                      builder.WithOrigins("http://localhost:8080",
                                                          "http://119.39.226.175:20157/")
                                      .AllowAnyHeader()
                                      .AllowAnyMethod();
                                  });
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Demo", Version = "v1" });
                c.OperationFilter<AddAuthTokenHeaderParameter>();
                var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);//获取应用程序所在目录（绝对，不受工作目录影响，建议采用此方法获取路径）
                var xmlPath = Path.Combine(basePath, "SwaggerDemo.xml");
                c.IncludeXmlComments(xmlPath);
            });
            string virtualPath = Configuration["virtualPath"];

            if (!string.IsNullOrEmpty(virtualPath))
            {
                services.AddSingleton<CustomDynamicRouteValueTransformer>(new CustomDynamicRouteValueTransformer("virtualpath"));
            }
            var connectionString = Configuration.GetConnectionString("Database");
            var version = Configuration.GetConnectionString("Version");
            services.AddDbContext<RickDBConext>(options => options.UseMySql(connectionString, ServerVersion.Parse(version)));
            services.AddScoped<ISysuserService, SysuserService>();
            services.AddScoped<IAppuserService, AppuserService>();
            services.AddScoped<IFileService, FileService>();
            services.AddSingleton<IIdGeneratorService, SnowFlakeService>();

            services.AddScoped<ISyscompanyService, SyscompanyService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IFunctionService, FunctionService>();
            services.AddScoped<IRepositoryService, RepositoryService>();
            services.AddScoped<IExpressclaimService, ExpressclaimService>();
            services.AddScoped<ICourierService, CourierService>();
            services.AddScoped<ICurrencyService, CurrencyService>();
            services.AddScoped<INationService, NationService>();
            services.AddScoped<IChannelService, ChannelService>();
            services.AddScoped<IAgentService, AgentService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<IAppuseraddressService, AppuseraddressService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<IPackageOrderApplyService, PackageOrderApplyService>();
            services.AddScoped<IAppuseraccountService, AppuseraccountService>();
            services.AddScoped<IPackageorderapplyexpressService, PackageorderapplyexpressService>();
            services.AddScoped<IIncomeService, IncomeService>();
            services.AddScoped<IAccountsubjectService, AccountsubjectService>();
            services.AddScoped<IAgentFeeService, AgentFeeService>();
            services.AddScoped<ISysmenuService, SysmenuService>();
            services.AddScoped<ICurrencychangerateService, CurrencychangerateService>();
            services.AddScoped<IRunFeeService, RunFeeService>();
            services.AddScoped<ISyssettingService, SyssettingService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IAppnewService, AppnewService>();

            var redisConnectionString = Configuration.GetConnectionString("RedisConnection");
            var redisDbNum = Convert.ToInt32(Configuration.GetConnectionString("RedisDbNum"));
            services.AddSingleton(new RedisClientService(redisConnectionString, redisDbNum));

            services.AddControllersWithViews(configure =>
            {
                configure.Filters.Add<CustomExceptionFilterAttribute>();
                configure.Filters.Add<CustomAuthorizationFilterAttribute>();
            })
            .AddJsonOptions(options => {
                options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new LongToStringJsonConverter());
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
            string virtualPath = Configuration["virtualPath"];
            app.UseSwagger(c =>
            {
                if (!string.IsNullOrEmpty(virtualPath))
                {
                    c.PreSerializeFilters.Add((openapidocument, httprequest) =>
                    {
                        openapidocument.Servers = new List<OpenApiServer> { new OpenApiServer {
                            Url = $"/{virtualPath}"
                        } };
                    });
                    c.RouteTemplate = $"/{virtualPath}/swagger/{{documentName}}/swagger.json";
                }
            });
            app.UseSwaggerUI(c =>
            {
                if (!string.IsNullOrEmpty(virtualPath))
                {
                    c.RoutePrefix = $"{virtualPath}/swagger";
                    c.SwaggerEndpoint($"/{virtualPath}/swagger/v1/swagger.json", "API Demo v1");
                }
                else
                {
                    c.SwaggerEndpoint($"/swagger/v1/swagger.json", "API Demo v1");
                }
            });
            app.UseStaticFiles();
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(60),
            };

            app.UseWebSockets(webSocketOptions);

            app.UseRouting();

            app.UseCors(MyAllowSpecificOrigins);
            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDynamicControllerRoute<CustomDynamicRouteValueTransformer>("{virtualpath}/api/{controller=Home}/{action=Index}/{id?}");
                //endpoints.MapDynamicControllerRoute<CustomDynamicRouteValueTransformer>("{virtualpath}/api/{controller=Home}/{id?}");
                //endpoints.MapControllerRoute(
                //    name: "default",
                //    pattern: "{controller=Home}/{action=Index}/{id?}");
                //if (!string.IsNullOrEmpty(virtualPath))
                //{
                //    //endpoints.MapDynamicControllerRoute<CustomDynamicRouteValueTransformer>("{virtualpath}/api/{controller=Home}/{action=Index}/{id?}");
                //    endpoints.MapDynamicControllerRoute<CustomDynamicRouteValueTransformer>("{virtualpath}/api/{controller=Home}/{id?}");
                //}

                //endpoints.MapControllers();

            });
        }
    }
}
