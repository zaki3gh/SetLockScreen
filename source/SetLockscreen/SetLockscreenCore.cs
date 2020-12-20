using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.AccessCache;


namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  ロック画面自動切り替えの処理.
    /// </summary>
    public sealed class SetLockscreenCore : IDisposable
    {
        /// <summary>
        ///  ロック画面に次のアイテムを設定する.
        /// </summary>
        /// <returns></returns>
        public async Task SetNextItemToLockscreenAsync()
        {
            if (IsAutoSetSuppressedNow())
            {
                this.Result = SetLockscreenResult.IsSuppresseNow;
                return;
            }

            var appSettings = AppSettings.Instance;
            var numItems = appSettings.NumberOfItems;
            if (numItems <= 0)
            {
                this.Result = SetLockscreenResult.NoItemByIndex;
                return;
            }

            var next = appSettings.CurrentIndex + 1;
            if (next < 0)
            {
                this.Result = SetLockscreenResult.NoItemByIndex;
                return;
            }
            if (next >= numItems)
            {
                this.Result = SetLockscreenResult.NoItemByIndex;
                next = 0;
            }

            var itemState = appSettings.GetItemsState();
            if (itemState == null)
            {
                this.Result = SetLockscreenResult.NoItemByState;
                return;
            }

            try
            {
                foreach (var index in EnumerateNextIndex(next, numItems))
                {
                    if (this.cancellationTokenSource.IsCancellationRequested)
                    {
                        this.Result = SetLockscreenResult.Canceled;
                        return;
                    }

                    if (await SetItemToLockscreenAsync(index, itemState))
                    {
                        appSettings.CurrentIndex = index;
                        appSettings.TimeOfSetLockscreen = DateTimeOffset.UtcNow;
                        this.Result = SetLockscreenResult.Success;
                        return;
                    }
                }

                // 有効なファイルが見つからない
                //  とりあえず先頭にしておく
                //  (そうしておけばほかの画像になっていた時に今の画像に戻されるはず)
                appSettings.CurrentIndex = 0;
                this.Result = SetLockscreenResult.NoItemAfterSet;
            }
            // キャンセルされた
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken != this.cancellationTokenSource.Token)
                {
                    throw;
                }

                this.Result = SetLockscreenResult.Canceled;
                return;
            }
        }

        /// <summary>
        ///  実行結果.
        /// </summary>
        public SetLockscreenResult Result { get; private set; }

        /// <summary>
        ///  次のインデックスを列挙する.
        /// </summary>
        /// <param name="nextFirst"></param>
        /// <param name="numItems"></param>
        /// <returns></returns>
        /// <remarks>
        ///  次のインデックスのアイテムがなくなっているかもしれないので
        ///  最大で一周するまで試す.
        /// </remarks>
        private IEnumerable<int> EnumerateNextIndex(int nextFirst, int numItems)
        {
            for (int index = nextFirst; index < numItems; ++index)
            {
                yield return index;
            }
            for (int index = 0; index < nextFirst; ++index)
            {
                yield return index;
            }
        }

        /// <summary>
        /// 指定したインデックスのアイテムをロック画面に設定する.
        /// </summary>
        /// <param name="index">アイテムのインデックス</param>
        /// <param name="itemState">アイテムの設定</param>
        /// <returns>成功したら<c>true</c>。</returns>
        private async Task<bool> SetItemToLockscreenAsync(int index, IDictionary<string, object> itemState)
        {
            var itemSetting = LockscreenImageItemSettingHelper.DeserializeItemAt(index, itemState);
            if (itemSetting == null)
            {
                return false;
            }
            if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(itemSetting.Token))
            {
                return false;
            }

            StorageFile file;
            try
            {
                file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(itemSetting.Token).AsTask(this.cancellationTokenSource.Token);
                if (file == null)
                {
                    return false;
                }
            }
            catch (System.IO.IOException)
            {
                return false;
            }

            try
            {
                // LockscreenItemsPage.SetSelectedItemToLockscreenAsync()と同じように対処する
                bool succeedToSet = false;
                try
                {
                    await Windows.System.UserProfile.LockScreen.SetImageFileAsync(file).AsTask(this.cancellationTokenSource.Token);
                    succeedToSet = true;
                }
                catch (UnauthorizedAccessException)
                {
                    // succeedToSetの上のコメント参照
                }

                if (!succeedToSet)
                {
                    using (var strm = await file.OpenReadAsync())
                    {
                        await Windows.System.UserProfile.LockScreen.SetImageStreamAsync(strm);
                        succeedToSet = true;
                    }
                }

                return succeedToSet;
            }
            catch (Exception ex)
            {
                // Dashboardにある品質情報によると
                // BackgroundTask実行中にこれらの例外でクラッシュしている
                if ((uint)ex.HResult == 0x80070490u)
                {
                    return false;
                }
                // FORTMATETC構造体が無効
                if ((uint)ex.HResult == 0x80040064u)
                {
                    return false;
                }
                throw;
            }
        }

        /// <summary>
        ///  自動切り替え抑止時間中かどうかを確認する
        /// </summary>
        /// <returns>自動切り替え抑止時間中なら<c>true</c>を返す</returns>
        public static bool IsAutoSetSuppressedNow()
        {
            var appSettings = AppSettings.Instance;
            var setting = appSettings.GetMiscellaneousSetting();

            var o = DateTimeOffset.UtcNow - appSettings.TimeOfSetLockscreen;
            return o.TotalMinutes <= (double)setting.SuppressedDuration;
        }

        /// <summary>
        ///  キャンセルを実行する.
        /// </summary>
        public void Cancel()
        {
            if (this.cancellationTokenSource == null)
            {
                throw new ObjectDisposedException("cancellationTokenSource");
            }

            this.cancellationTokenSource.Cancel();
        }

        /// <summary>
        ///  <c>IDispose.Dispose()</c>の実装.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///  <c>Dispose</c>パターンによる実装.
        /// </summary>
        /// <param name="disposing"><c>Dispose()</c>から呼ばれたかどうかを示す</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.cancellationTokenSource != null)
                {
                    this.cancellationTokenSource.Dispose();
                    this.cancellationTokenSource = null;
                }
            }
        }

        /// <summary>
        ///  アイテムの登録情報の変更をキャンセルするための<c>CancellationTokenSource</c>.
        /// </summary>
        private System.Threading.CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    ///  ロック画面切り替えの実行結果.
    /// </summary>
    public enum SetLockscreenResult
    {
        /// <summary>未設定</summary>
        NotSet,

        /// <summary>切り替え成功</summary>
        Success,

        /// <summary>切り替え抑止中</summary>
        IsSuppresseNow,

        /// <summary>キャンセルされた</summary>
        Canceled,

        /// <summary>設定するべきインデックスが不正でアイテム見つからず</summary>
        NoItemByIndex,

        /// <summary>アイテムの登録情報が不正でアイテム見つからず</summary>
        NoItemByState,

        /// <summary>アイテムの登録情報内に有効なアイテムがなかった</summary>
        NoItemAfterSet, 
    }
}
