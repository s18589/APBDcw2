using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Cwiczenia3.DAL;
using Cwiczenia3.Middlewares;
using Cwiczenia3.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cwiczenia3
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
            services.AddSingleton<IStudentDbService, SqlServerStudentDbService>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<LoggingMiddleware>();

            app.Use(async (context, next) =>
            {
                if (!context.Request.Headers.ContainsKey("Index"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Ni ma indexu");
                    return;
                }
                string index = context.Request.Headers["Index"].ToString();

                using(var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18589;Integrated Security=true"))
                using(var command = new SqlCommand())
                {
                    connection.Open();
                    command.Connection = connection;
                    var transaction = connection.BeginTransaction();
                    command.Transaction = transaction;

                    command.Parameters.AddWithValue("index", index);
                    command.CommandText = "select index from student where indexnumber = index";

                    var dr = command.ExecuteReader();

                    if (!dr.HasRows)
                    {
                        dr.Close();
                        transaction.Rollback();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Ni ma indexu w bazie");
                        return;
                    }
                }
                await next();
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
