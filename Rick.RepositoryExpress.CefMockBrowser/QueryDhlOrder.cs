using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.Text;
using CefSharp;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.CefMockBrowser
{
    public class QueryDhlOrder
    {
        public QueryDhlOrder(string orderNumber)
        {
            this.QueryUrl = "https://mydhlplus.dhl.com/cn/zh/tracking.html#/track-by-reference";
            this.OrderNumber = orderNumber;
        }
        private string QueryUrl { get; set; }
        private string OrderNumber { get; set; }
        private bool HasCompleted { get; set; }
        private ChromiumWebBrowser browser { get; set; }
        public void TryQuery()
        {
            browser = new ChromiumWebBrowser(QueryUrl);
            // This returns to us from another thread.
            browser.LoadingStateChanged += BrowserLoadingStateChanged;

            // Clean up Chromium objects.  You need to call this in your application otherwise
            // you will get a crash when closing.
            while (!HasCompleted)
            {
                Task.Delay(10000).Wait();
            }
            Console.WriteLine("Hello World!");

        }

        private async void BrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            // Check to see if loading is complete - this event is called twice, one when loading starts
            // second time when it's finished
            // (rather than an iframe within the main frame).
            if (!e.IsLoading)
            {
                // Remove the load event handler, because we only want one snapshot of the initial page.
                browser.LoadingStateChanged -= BrowserLoadingStateChanged;

                //确认已加载完成
                var scriptTask = await browser.EvaluateScriptAsync("document.querySelector('input[name=fromDate]').value;");
                while (scriptTask.Result == null)
                {
                    scriptTask = await browser.EvaluateScriptAsync("document.querySelector('input[name=fromDate]').value;");
                }
                Task.Delay(1500).Wait();

                var clickTask = await browser.EvaluateScriptAsync("document.querySelector('[link-text=查询]').click()");
                Task.Delay(1500).Wait();

                StringBuilder orderInsertAndChangeC = new StringBuilder();
                orderInsertAndChangeC.Append(" document.querySelector('[name=trackingNumbers]').value='" + OrderNumber + "';");
                var orderInsertTask = await browser.EvaluateScriptAsync(orderInsertAndChangeC.ToString());
                Task.Delay(1500).Wait();

                {
                    var taskscreenshot = await browser.ScreenshotAsync();
                    var screenshotPath = Path.Combine("E:\\CefSharpTest\\", Guid.NewGuid().ToString("N") + ".png");

                    taskscreenshot.Save(screenshotPath);

                    // We no longer need the Bitmap.
                    // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
                    taskscreenshot.Dispose();
                    Console.WriteLine("Screenshot saved.  Launching your default image viewer...");
                    browser.Dispose();
                    HasCompleted = true;
                }

                //scriptTask.ContinueWith(t =>
                //{
                //    Task.Delay(10000).Wait();
                //    StringBuilder orderInsertAndChangeC = new StringBuilder();
                //    orderInsertAndChangeC.Append(" document.querySelector('[name=trackingNumbers]').value='" + OrderNumber + "';");
                //    //orderInsertAndChangeC.Append(" var cchangeEvent = document.createEvent('HTMLEvents');");
                //    //orderInsertAndChangeC.Append(" cchangeEvent.initEvent('change', true, true);");
                //    //orderInsertAndChangeC.Append(" document.querySelector('[name=trackingNumbers]').dispatchEvent(cchangeEvent);");
                //    var orderInsertTask = browser.EvaluateScriptAsync(orderInsertAndChangeC.ToString());
                //    orderInsertTask.ContinueWith(o =>
                //    {
                //        Task.Delay(10000).Wait();

                //        StringBuilder orderInsertAndChangeC2 = new StringBuilder();
                //        orderInsertAndChangeC2.Append(" var cchangeEvent = document.createEvent('HTMLEvents');");
                //        var orderInsertTask2 = browser.EvaluateScriptAsync(orderInsertAndChangeC2.ToString());
                //        orderInsertTask2.ContinueWith(o =>
                //        {
                //            Task.Delay(10000).Wait();

                //            StringBuilder orderInsertAndChangeC3 = new StringBuilder();
                //            orderInsertAndChangeC3.Append(" cchangeEvent.initEvent('change', true, true);");
                //            var orderInsertTask3 = browser.EvaluateScriptAsync(orderInsertAndChangeC3.ToString());
                //            orderInsertTask3.ContinueWith(o =>
                //            {
                //                Task.Delay(10000).Wait();

                //                StringBuilder orderInsertAndChangeC4 = new StringBuilder();
                //                orderInsertAndChangeC4.Append(" document.querySelector('[name=trackingNumbers]').dispatchEvent(cchangeEvent);");
                //                var orderInsertTask4 = browser.EvaluateScriptAsync(orderInsertAndChangeC4.ToString());
                //                orderInsertTask4.ContinueWith(o =>
                //                {
                //                    Task.Delay(10000).Wait();
                //                    var queryTask = browser.EvaluateScriptAsync("document.querySelector('[aqa-id=submitTrackingNumbersBtn]').click();");
                //                    queryTask.ContinueWith(q =>
                //                    {
                //                        Task.Delay(10000).Wait();
                //                        var task = browser.ScreenshotAsync();
                //                        task.ContinueWith(x =>
                //                        {
                //                            // Make a file to save it to (e.g. C:\Users\jan\Desktop\CefSharp screenshot.png)
                //                            //var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");
                //                            var screenshotPath = Path.Combine("E:\\CefSharpTest\\", Guid.NewGuid().ToString("N") + ".png");

                //                            Console.WriteLine();
                //                            Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

                //                            // Save the Bitmap to the path.
                //                            // The image type is auto-detected via the ".png" extension.
                //                            task.Result.Save(screenshotPath);

                //                            // We no longer need the Bitmap.
                //                            // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
                //                            task.Result.Dispose();

                //                            Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

                //                            // Tell Windows to launch the saved image.
                //                            //Process.Start(new ProcessStartInfo(screenshotPath)
                //                            //{
                //                            //    // UseShellExecute is false by default on .NET Core.
                //                            //    UseShellExecute = true
                //                            //});
                //                            Console.WriteLine("Image viewer launched.  Press any key to exit.");
                //                            browser.Dispose();
                //                            HasCompleted = true;
                //                        });

                //                    });
                //                });

                //            });


                //        });

                //    });
                //});
            }
        }

    }
}
