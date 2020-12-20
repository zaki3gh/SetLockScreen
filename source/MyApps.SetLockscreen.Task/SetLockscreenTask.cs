using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace MyApps.SetLockscreen.BackgroundTask
{
    /// <summary>
    ///  ロック画面切り替えのバックグラウンドタスク.
    /// </summary>
    public sealed class SetLockscreenTask : IBackgroundTask
    {
        /// <summary>
        ///  <c>IBackgroundTask.Run()</c>の実装.
        /// </summary>
        /// <param name="taskInstance"></param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += taskInstance_Canceled;

            using (this.setLockscreenCore = new SetLockscreenCore())
            {
                await this.setLockscreenCore.SetNextItemToLockscreenAsync();
                taskInstance.Canceled -= taskInstance_Canceled;

#if DEBUG
                await WriteLogAsync(taskInstance, this.setLockscreenCore.Result);
#endif
            }

            deferral.Complete();
        }

        /// <summary>
        ///  バックグラウンドタスクがキャンセルされた.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reason"></param>
        void taskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (this.setLockscreenCore != null)
            {
                this.setLockscreenCore.Cancel();
            }
        }

        /// <summary>
        ///  ロック画面切り替えの処理.
        /// </summary>
        private SetLockscreenCore setLockscreenCore;

#if DEBUG

        /// <summary>
        ///  動作ログを出力する.
        /// </summary>
        /// <param name="taskInstance"></param>
        /// <returns></returns>
        async Task WriteLogAsync(IBackgroundTaskInstance taskInstance, SetLockscreenResult result)
        {
            var log = String.Format("{0}: {1} / {2}, {3}{4}",
                DateTimeOffset.Now,
                taskInstance.Task.Name,
                taskInstance.Task.TaskId,
                result, 
                Environment.NewLine);
            System.Diagnostics.Debug.WriteLine(log);

            var logfile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("bgtask.log", Windows.Storage.CreationCollisionOption.OpenIfExists);
            if (logfile != null)
            {
                System.Diagnostics.Debug.WriteLine(logfile.Path);
                using (var strm = await logfile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    strm.Seek(strm.Size);
                    using (var dw = new Windows.Storage.Streams.DataWriter(strm))
                    {
                        dw.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf16LE;
                        dw.WriteString(log);
                        await dw.StoreAsync();
                        await dw.FlushAsync();
                    }
                }
            }
        }

#endif
    }
}
