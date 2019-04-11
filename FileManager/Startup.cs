using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FileManager.Helpers;
using FileManager.Services;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace FileManager
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddCors();
                // соединение с бд
                string connection = Configuration.GetConnectionString("MSConnection");
                services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connection));
                services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
                services.AddAutoMapper();

                var appSettingsSection = Configuration.GetSection("AppSettings");
                services.Configure<AppSettings>(appSettingsSection);

                var appSettings = appSettingsSection.Get<AppSettings>();
                var key = Encoding.ASCII.GetBytes(appSettings.Secret);

                services.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                            var userId = int.Parse(context.Principal.Identity.Name);
                            var user = userService.GetById(userId);
                            if (user == null)
                            {
                                context.Fail("Пользователь не авторизован");
                            }
                            return Task.CompletedTask;
                        }
                    };
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

                services.AddScoped<IUserService, UserService>();
            }
            catch(Exception ex)
            {}
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddDebug();
            loggerFactory.AddConsole();

            app.UseCors(x => x
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

            //app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}