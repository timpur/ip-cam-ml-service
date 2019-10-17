using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace IPCamMLService
{
    public class MJEPGImageSource : IImageSource, IDisposable
    {
        public string Name { get; }
        public string Source { get; }

        private HttpClient Client { get; }
        private delegate void OnFrame(Bitmap frame);
        private event OnFrame OnFrameEvent;
        private Task ListeningTask;
        private CancellationTokenSource ListeningTaskCancellationTokenSource = new CancellationTokenSource();

        public MJEPGImageSource(string name, string source)
        {
            Name = name;
            Source = source;

            Client = new HttpClient();
        }

        public async IAsyncEnumerable<Bitmap> GetStream(string url)
        {
            if (ListeningTask == null) Start();

            var queue = new Queue<Bitmap>();
            OnFrame onFrame = (Bitmap image) => queue.Enqueue(image);
            OnFrameEvent += onFrame;


        }

        private void Start()
        {
            ListeningTask = Task.Run(async () =>
             {
                 using var result = await Client.GetAsync(Source, HttpCompletionOption.ResponseHeadersRead);
                 var content_type = result.Content.Headers.GetValues("Content-type").First();
                 var rx = new Regex(@"multipart/x-mixed-replace; boundary=""(.*)""");
                 var match = rx.Match(content_type);
                 if (match != null)
                 {
                     var boundary = match.Groups[1].Value;
                     var reader = new MultipartReader(boundary, await result.Content.ReadAsStreamAsync());
                     MultipartSection section;
                     while ((section = await reader.ReadNextSectionAsync()) != null || !ListeningTaskCancellationTokenSource.IsCancellationRequested)
                     {
                         var image = new Bitmap(section.Body);
                         OnFrameEvent.Invoke(image);
                     }
                 }
             }, ListeningTaskCancellationTokenSource.Token);
        }

        public void Dispose()
        {
            ListeningTaskCancellationTokenSource.Cancel();
        }
    }
}