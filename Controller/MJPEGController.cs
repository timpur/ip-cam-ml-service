using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IPCamMLService.Models;
using System.Net.Http.Headers;

namespace IPCamMLService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MJPEGController : ControllerBase
    {
        IImageSource Source = new IPCamMLService.Models.MJPEGImageSource("Cam1", "http://202.90.241.79:82/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER", 15);

        [HttpGet]
        public MultipartResult Get()
        {
            return new MultipartResult(Source.GetStream().Select(frame =>
            {
                var content = new ByteArrayContent(GetImageAsBytes(frame.Image));
                content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                return content;
            }));
        }

        private byte[] GetImageAsBytes(Bitmap Image, int quality = 100)
        {
            var codec = ImageCodecInfo.GetImageDecoders().First(x => x.MimeType == "image/jpeg");
            var encoderParams = new EncoderParameters
            {
                Param = new EncoderParameter[]{
                    new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality)
                }
            };
            using var stream = new MemoryStream();
            Image.Save(stream, codec, encoderParams);
            Console.WriteLine($"2: {stream.Length}");
            return stream.GetBuffer();
        }
    }

    public class MultipartResult : IActionResult
    {
        public IAsyncEnumerable<HttpContent> ContentParts { get; }
        public string Boundary { get; }

        public MultipartResult(IAsyncEnumerable<HttpContent> contentParts, string boundary = "multipart-boundary")
        {
            ContentParts = contentParts;
            Boundary = boundary;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var Response = context.HttpContext.Response;

            Response.ContentType = $"multipart/x-mixed-replace; boundary=\"${Boundary}\"";
            await Response.StartAsync();

            await foreach (var content in ContentParts)
            {
                if (context.HttpContext.RequestAborted.IsCancellationRequested) break;

                await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"--${Boundary}\n"));
                await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(content.Headers.ToString() + "\n"));
                await Response.BodyWriter.WriteAsync(await content.ReadAsByteArrayAsync());
                await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes("\n"));
                await Response.BodyWriter.FlushAsync();
            }
        }
    }
}