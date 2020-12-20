using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Windows.Data.Json;
using Windows.ApplicationModel.Background;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  ロック画面切り替え設定の拡張.
    /// </summary>
    public static class UpdateTriggerExtension
    {
        /// <summary>
        ///  バックグラウンドタスクのエントリポイント.
        /// </summary>
        const string TaskEntryPoint = "MyApps.SetLockscreen.BackgroundTask.SetLockscreenTask";

        /// <summary>
        ///  バックグラウンドタスクの登録を更新する.
        /// </summary>
        /// <param name="updateTrigger">バックグラウンドタスクとして登録するロック画面切り替え設定</param>
        /// <returns></returns>
        public static BackgroundTaskRegistration UpdateBackgroundTaskRegistration(this UpdateTriggerBase updateTrigger)
        {
            if (updateTrigger == null)
            {
                throw new ArgumentNullException("updateTrigger");
            }

            var trigger = updateTrigger.MakeBackgroundTrigger();
            if (trigger == null)
            {
                throw new NotSupportedException();
            }

            // タスクの更新はないので既存タスクを一度登録解除する必要がある
            Unregister(updateTrigger.TaskName);
            if (updateTrigger.IsOn)
            {
                return Register(updateTrigger.TaskName, trigger);
            }
            else
            {
                // 解除済みなので何もする必要はなくただnullを返す
                return null;
            }
        }

        /// <summary>
        ///  バックグラウンドタスクを登録する.
        /// </summary>
        /// <param name="taskName">タスク名</param>
        /// <param name="taskEntryPoint">タスクのエントリポイント</param>
        /// <param name="trigger">タスクのトリガー</param>
        /// <returns>登録されたタスクの<c>BackgroundTaskRegistration</c></returns>
        private static BackgroundTaskRegistration Register(string taskName, IBackgroundTrigger trigger)
        {
            if (String.IsNullOrEmpty(taskName))
            {
                throw new ArgumentNullException("taskName");
            }
            if (trigger == null)
            {
                throw new ArgumentNullException("trigger");
            }

            var builder = new BackgroundTaskBuilder();
            builder.Name = taskName;
            builder.TaskEntryPoint = UpdateTriggerExtension.TaskEntryPoint;
            builder.SetTrigger(trigger);
            return builder.Register();
        }

        /// <summary>
        ///  バックグラウンドタスクを登録解除する.
        /// </summary>
        /// <param name="taskName">タスク名</param>
        private static void Unregister(string taskName)
        {
            if (String.IsNullOrEmpty(taskName))
            {
                throw new ArgumentNullException("taskName");
            }

            var task = BackgroundTaskRegistration.AllTasks.FirstOrDefault(
                x => x.Value.Name.Equals(taskName, StringComparison.Ordinal));
            if (task.Value != null)
            {
                task.Value.Unregister(true);
            }
        }

    }

}
