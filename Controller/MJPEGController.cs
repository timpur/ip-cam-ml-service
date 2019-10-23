using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IPCamMLService.Models;


namespace IPCamMLService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MJPEGController : ControllerBase
    {
        IImageSource Source = new IPCamMLService.Models.MJPEGImageSource("Cam1", "http://202.90.241.79:82/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER", 15);

        // [HttpGet()]
        // public async Task<Stream> Get()
        // {
        //     var source = new IPCamMLService.Models.MJEPGImageSource("Cam1", "http://202.90.241.79:82/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER", 15);
        //     var result = new MultipartContent("x-mixed-replace", "test-test");

        //     Task.Run(async () =>
        //     {
        //         await foreach (var frame in source.GetStream())
        //         {
        //             var stream = new MemoryStream();
        //             frame.Image.Save(stream, ImageFormat.Jpeg);
        //             stream.Position = 0;
        //             var content = new StreamContent(stream);
        //             result.Add(content);
        //         }
        //     });

        //     await Task.Delay(1000);
        //     Response.ContentType = result.Headers.ContentType.ToString();
        //     return await result.ReadAsStreamAsync();
        // }

        [HttpGet]
        public async Task Get()
        {
            Response.ContentType = "multipart/x-mixed-replace; boundary=\"ml-image\"";
            await Response.StartAsync();
            var t = 0;
            await foreach (var frame in Source.GetStream())
            {
                if (HttpContext.RequestAborted.IsCancellationRequested) break;

                t += 1;
                if (t == 2) break;

                var stream = new MemoryStream();
                frame.Image.Save(stream, ImageFormat.Jpeg);
                stream.Position = 0;
                var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("--ml-image" + "\n"));
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(content.Headers.ToString() + "\n"));
                await content.CopyToAsync(Response.Body);
                var test = Encoding.UTF8.GetString(frame.GetImageAsBytes());
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("\n"));
                await Response.Body.FlushAsync();
            }
        }
    }
}