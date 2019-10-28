using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using IPCamMLService.Models;

namespace IPCamMLService.ImageSource
{
    public class MJPEGImageSource : IImageSource, IDisposable
    {
        public string Name { get; }
        public string Source { get; }
        public int FPS { get; }

        private HttpClient Client { get; }
        private Task StreamTask;
        private CancellationTokenSource StreamCancellation = new CancellationTokenSource();
        private int StreamListerners = 0;
        private Frame CurrentFrame;


        public MJPEGImageSource(string name, string source, int fps)
        {
            Name = name;
            Source = source;
            FPS = fps;

            Client = new HttpClient();
        }

        public async IAsyncEnumerable<IFrame> GetStream()
        {
            try
            {
                Start();
                var delay = TimeSpan.FromSeconds(1) / FPS;

                // TODO: FPS delay max is rate of source or less

                while (!StreamTask.IsCompleted)
                {
                    if (CurrentFrame != null) yield return CurrentFrame;
                    await Task.Delay(delay);
                };
            }
            finally
            {
                if (StreamTask.IsFaulted) throw StreamTask.Exception;

                Stop();
            }
        }

        private void Start()
        {
            lock ((object)StreamListerners)
            {
                StreamListerners += 1;
                if (StreamListerners > 0 && StreamTask == null)
                {
                    StreamTask = Task.Run(async () =>
                    {
                        using var result = await Client.GetAsync(Source, HttpCompletionOption.ResponseHeadersRead);
                        result.EnsureSuccessStatusCode();
                        var contentType = result.Content.Headers.GetValues("Content-type").First();
                        var boundary = GetBoundary(contentType);
                        var reader = new MultipartReader(boundary, await result.Content.ReadAsStreamAsync());
                        MultipartSection section;
                        while ((section = await reader.ReadNextSectionAsync()) != null && !StreamCancellation.IsCancellationRequested)
                        {
                            var image = new Bitmap(section.Body);
                            CurrentFrame = new Frame(image);
                        }

                    }, StreamCancellation.Token);
                }
            }
        }

        private void Stop()
        {
            lock ((object)StreamListerners)
            {
                StreamListerners -= 1;
                if (StreamListerners == 0 && StreamTask != null)
                {
                    StreamCancellation.Cancel();
                    StreamTask = null;
                }
            }
        }

        public void Dispose()
        {
            StreamCancellation.Cancel();
        }


        private string GetBoundary(string contentType)
        {
            var match = new Regex(@"multipart/x-mixed-replace; boundary=""(.*)""").Match(contentType);
            if (match != null)
            {
                return match.Groups[1].Value;
            }

            throw new Exception("Boundary Not Found.");
        }
    }
}