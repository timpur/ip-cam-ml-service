using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Drawing;

namespace IPCamMLService
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            Test().Wait();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }

        async Task Test()
        {
            var client = new HttpClient();
            var result = await client.GetAsync("http://202.90.241.79:82/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER", HttpCompletionOption.ResponseHeadersRead);
            var content_type = result.Content.Headers.GetValues("Content-type").First();
            var rx = new Regex(@"multipart/x-mixed-replace; boundary=""(.*)""");
            var match = rx.Match(content_type);
            if (match != null)
            {
                var boundary = match.Groups[1].Value;
                var reader = new MultipartReader(boundary, await result.Content.ReadAsStreamAsync());
                MultipartSection section;
                while ((section = await reader.ReadNextSectionAsync()) != null)
                {
                    var image = new Bitmap(section.Body);
                }
            }
        }


        async Task Test2()
        {
            var client = new HttpClient();
            var result = await client.GetAsync("http://202.90.241.79:82/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER", HttpCompletionOption.ResponseHeadersRead);
            var content_type = result.Content.Headers.GetValues("Content-type").First();
            var rx = new Regex(@"multipart/x-mixed-replace; boundary=""(.*)""");
            var match = rx.Match(content_type);
            if (match != null)
            {
                var boundary = match.Groups[1].Value;
                var stream = await result.Content.ReadAsStreamAsync();
                var streamReader = new StreamReader(stream);
                string frame = "";
                string buffer = "";
                while ((buffer = await streamReader.ReadLineAsync()) != null)
                {
                    if (buffer.StartsWith($"--{boundary}"))
                    {
                        // var type = await streamReader.ReadLineAsync();
                        // var size = await streamReader.ReadLineAsync();
                        // var empty = await streamReader.ReadLineAsync();
                        // var data = byte[]
                        frame = "";
                    }
                    else
                    {
                        frame += buffer;
                    }
                }
            }

        }
    }
}
