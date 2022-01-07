using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CefSharp.OffScreen;
using CefSharp;
using System.Collections.Generic;
using Rick.RepositoryExpress.Common;
//using Newtonsoft.Json;
using System.Text.Json;

namespace Rick.RepositoryExpress.CefMockBrowser
{
    class Program
    {
        private static ChromiumWebBrowser browser;
        static void Main(string[] args)
        {
            string date = DateTime.Now.ToString("ddHHmmssfff");
            Console.WriteLine(date);
            Console.ReadLine();
            //var obj = new { id= 1468118496494882816 };
            ////string res = JsonConvert.SerializeObject(obj);
            //string res = JsonSerializer.Serialize(obj);
            //Console.WriteLine(res);
            ////SnowFlakeService snowFlakeService = new SnowFlakeService();
            ////List<long> ids = new List<long>();
            ////for (int i = 0; i < 100; i++)
            ////{
            ////    long id = snowFlakeService.NextId();
            ////    if (ids.Contains(id))
            ////    {
            ////        Console.WriteLine("产生重复ID");
            ////    }
            ////    else
            ////    {
            ////        ids.Add(id);
            ////    }
            ////}
            //var settings = new CefSettings()
            //{
            //    //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
            //    CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            //};
            //Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            //List<QueryDhlOrder> QueryDhlOrderS = new List<QueryDhlOrder>();
            //for (int i = 0; i < 10; i++)
            //{
            //    QueryDhlOrder queryDhlOrder = new QueryDhlOrder("3826859713");
            //    queryDhlOrder.TryQuery();
            //}
            ////Parallel.For(1, 2, i =>
            ////{
            ////    QueryDhlOrder queryDhlOrder = new QueryDhlOrder("3826859713");
            ////    queryDhlOrder.TryQuery();
            ////});
            //Cef.Shutdown();
            //Console.ReadKey();

            ////Parallel.For(1,100,i=> {
            ////    QueryDhlOrder queryDhlOrder = new QueryDhlOrder("3826859713");
            ////    queryDhlOrder.TryQuery();
            ////    Console.ReadKey();
            ////});
        }

        #region 注释

        //static void Main(string[] args)
        //{
        //    const string testUrl = "https://mydhlplus.dhl.com/cn/zh/tracking.html#/track-by-reference";
        //    var settings = new CefSettings()
        //    {
        //        //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
        //        CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
        //    };
        //    Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        //    browser = new ChromiumWebBrowser(testUrl);

        //    // An event that is fired when the first page is finished loading.
        //    // This returns to us from another thread.
        //    browser.LoadingStateChanged += BrowserLoadingStateChanged;

        //    // We have to wait for something, otherwise the process will exit too soon.
        //    Console.ReadKey();

        //    // Clean up Chromium objects.  You need to call this in your application otherwise
        //    // you will get a crash when closing.
        //    Cef.Shutdown();

        //    Console.WriteLine("Hello World!");
        //}

        //private static void BrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        //{
        //    // Check to see if loading is complete - this event is called twice, one when loading starts
        //    // second time when it's finished
        //    // (rather than an iframe within the main frame).
        //    if (!e.IsLoading)
        //    {
        //        // Remove the load event handler, because we only want one snapshot of the initial page.
        //        browser.LoadingStateChanged -= BrowserLoadingStateChanged;

        //        var scriptTask = browser.EvaluateScriptAsync("document.getElementById('J_SearchInput').value='YT5804229368499'");

        //        #region 测试代码
        //        //var scriptTask = browser.EvaluateScriptAsync("document.querySelector('[name=q]').value = 'CefSharp Was Here!'");
        //        //scriptTask.ContinueWith(s=> {
        //        //    Thread.Sleep(500);
        //        //    var queryTask = browser.EvaluateScriptAsync("document.getElementById('J_SearchInput').value");
        //        //    queryTask.ContinueWith(q=> {
        //        //        Thread.Sleep(500);
        //        //        var r = q.Result;
        //        //    });
        //        //});
        //        #endregion

        //        scriptTask.ContinueWith(t =>
        //        {
        //            Thread.Sleep(500);
        //            var clickTask = browser.EvaluateScriptAsync("document.getElementById('J_SearchBtn').click()");
        //            clickTask.ContinueWith(c =>
        //            {
        //                Thread.Sleep(500);
        //                var queryTask = browser.EvaluateScriptAsync("document.querySelectorAll('#J_PackageDetail li').length");
        //                queryTask.ContinueWith(q =>
        //                {
        //                    Thread.Sleep(100);
        //                    var jr = queryTask.Result;
        //                    var hr = jr.Result;
        //                    var task = browser.ScreenshotAsync();
        //                    task.ContinueWith(x =>
        //                    {
        //                        // Make a file to save it to (e.g. C:\Users\jan\Desktop\CefSharp screenshot.png)
        //                        var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

        //                        Console.WriteLine();
        //                        Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

        //                        // Save the Bitmap to the path.
        //                        // The image type is auto-detected via the ".png" extension.
        //                        task.Result.Save(screenshotPath);

        //                        // We no longer need the Bitmap.
        //                        // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
        //                        task.Result.Dispose();

        //                        Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

        //                        // Tell Windows to launch the saved image.
        //                        Process.Start(new ProcessStartInfo(screenshotPath)
        //                        {
        //                            // UseShellExecute is false by default on .NET Core.
        //                            UseShellExecute = true
        //                        });
        //                        Console.WriteLine("Image viewer launched.  Press any key to exit.");
        //                    }, TaskScheduler.Default);

        //                });

        //                //var queryTask = browser.EvaluateScriptAsync("document.getElementById('J_PackageDetail').innerHTML");
        //                //queryTask.ContinueWith(q => {
        //                //    Thread.Sleep(500);
        //                //    var jr = queryTask.Result;
        //                //    var hr = jr.Result;

        //                //});

        //                //var task = browser.ScreenshotAsync();
        //                //task.ContinueWith(x =>
        //                //{
        //                //    // Make a file to save it to (e.g. C:\Users\jan\Desktop\CefSharp screenshot.png)
        //                //    var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

        //                //    Console.WriteLine();
        //                //    Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

        //                //    // Save the Bitmap to the path.
        //                //    // The image type is auto-detected via the ".png" extension.
        //                //    task.Result.Save(screenshotPath);

        //                //    // We no longer need the Bitmap.
        //                //    // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
        //                //    task.Result.Dispose();

        //                //    Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

        //                //    // Tell Windows to launch the saved image.
        //                //    Process.Start(new ProcessStartInfo(screenshotPath)
        //                //    {
        //                //        // UseShellExecute is false by default on .NET Core.
        //                //        UseShellExecute = true
        //                //    });

        //                //    Console.WriteLine("Image viewer launched.  Press any key to exit.");
        //                //}, TaskScheduler.Default);
        //            });
        //            //Give the browser a little time to render
        //            // Wait for the screenshot to be taken.
        //        });
        //    }
        //}
        #endregion
    }
}
