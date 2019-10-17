using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace IPCamMLService.Models
{
    public class MJEPGImageSource : IImageSource, IDisposable
    {
        public string Name { get; }
        public string Source { get; }
        public int FPS { get; }

        private HttpClient Client { get; }
        private Task StreamTask;
        private Bitmap CurrentFrame;
        private CancellationTokenSource StreamCancellation = new CancellationTokenSource();

        public MJEPGImageSource(string name, string source, int fps)
        {
            Name = name;
            Source = source;
            FPS = fps;

            Client = new HttpClient();
        }

        public async IAsyncEnumerable<Bitmap> GetStream()
        {
            if (StreamTask == null) Start();
            var delay = TimeSpan.FromSeconds(1) / FPS;

            while (!StreamTask.IsCompleted)
            {
                if (CurrentFrame != null) yield return CurrentFrame;
                await Task.Delay(delay);
            };

            if (StreamTask.IsFaulted) throw StreamTask.Exception;
        }

        private void Start()
        {
            StreamTask = Task.Run(async () =>
             {
                 using var result = await Client.GetAsync(Source, HttpCompletionOption.ResponseHeadersRead);
                 result.EnsureSuccessStatusCode();
                 var content_type = result.Content.Headers.GetValues("Content-type").First();
                 var rx = new Regex(@"multipart/x-mixed-replace; boundary=""(.*)""");
                 var match = rx.Match(content_type);
                 if (match != null)
                 {
                     var boundary = match.Groups[1].Value;
                     var reader = new MultipartReader(boundary, await result.Content.ReadAsStreamAsync());
                     MultipartSection section;
                     while ((section = await reader.ReadNextSectionAsync()) != null || !StreamCancellation.IsCancellationRequested)
                     {
                         var image = new Bitmap(section.Body);
                         CurrentFrame = image;
                     }
                 }
             }, StreamCancellation.Token);
        }

        private void Stop(){
            StreamCancellation.Cancel();
            StreamTask.IsCompletedSuccessfully
        }

        public void Dispose()
        {
            StreamCancellation.Cancel();
        }
    }
}