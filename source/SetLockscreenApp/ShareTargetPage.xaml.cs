using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;

using System.Runtime.InteropServices.WindowsRuntime;

// 共有ターゲット コントラクトのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234241 を参照してください

namespace MyApps.SetLockscreen
{
    /// <summary>
    /// このページを使用すると、他のアプリケーションがこのアプリケーションを介してコンテンツを共有できます。
    /// </summary>
    public sealed partial class ShareTargetPage : Common.LayoutAwarePage, IDisposable
    {
        /// <summary>
        /// 共有操作について、Windows と通信するためのチャネルを提供します。
        /// </summary>
        private Windows.ApplicationModel.DataTransfer.ShareTarget.ShareOperation _shareOperation;

        /// <summary>
        ///  Constructor.
        /// </summary>
        public ShareTargetPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 他のアプリケーションがこのアプリケーションを介してコンテンツの共有を求めた場合に呼び出されます。
        /// </summary>
        /// <param name="args">Windows と連携して処理するために使用されるアクティベーション データ。</param>
        public void Activate(ShareTargetActivatedEventArgs args)
        {
            this._shareOperation = args.ShareOperation;

            // ビュー モデルを使用して、共有されるコンテンツのメタデータを通信します
            var shareProperties = this._shareOperation.Data.Properties;
            this.DefaultViewModel["Title"] = shareProperties.Title;
            this.DefaultViewModel["Description"] = shareProperties.Description;
            this.DefaultViewModel["Sharing"] = false;
            Window.Current.Content = this;
            Window.Current.Activate();

            // 共有データを選択するためのリストとサムネイルを準備する
            this.DefaultViewModel["SharedItems"] = this.sharedItems;
            AddSharedItemsAsync(this._shareOperation);
        }

        /// <summary>
        /// ユーザーが [共有] をクリックしたときに呼び出されます。
        /// </summary>
        /// <param name="sender">共有を開始するときに使用される Button インスタンス。</param>
        /// <param name="e">ボタンがどのようにクリックされたかを説明するイベント データ。</param>
        private async void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            this.DefaultViewModel["Sharing"] = true;
            this._shareOperation.ReportStarted();

            // TODO: this._shareOperation.Data を使用して共有シナリオに適した
            //       作業を実行します。通常は、カスタム ユーザー インターフェイス要素を介して
            //       このページに追加されたカスタム ユーザー インターフェイス要素を介して
            //       this.DefaultViewModel["Comment"]

            // リストビューで選択されているアイテムを登録する
            try
            {
                // まずは現在の登録リストをロード
                await App.Current.LoadAllItemsAsync();

                bool emptyBefore = App.Current.LockscreenItemList.IsEmpty;

                foreach (var item in this.SharedItemsView.SelectedItems)
                {
                    var sharedItem = item as ISharedItem;
                    if (sharedItem.File == null)
                    {
                        await sharedItem.PrepareFileAsync();
                        if (sharedItem.File == null)
                        {
                            continue;
                        }
                    }
                    await App.Current.AddFileAsync(sharedItem.File, sharedItem.IsTemporary, this.cancellationTokenSource.Token);
                }

                if (emptyBefore && !App.Current.LockscreenItemList.IsEmpty)
                {
                    App.Current.UpdateBackgroundTaskRegistration();
                }
            }
            // キャンセルされた
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken != this.cancellationTokenSource.Token)
                {
                    throw;
                }
            }
            finally
            {
                this._shareOperation.ReportCompleted();
            }
        }

        /// <summary>
        ///  共有データに含まれる画像とファイルを登録候補として追加する.
        /// </summary>
        /// <param name="shareOperation"></param>
        private async void AddSharedItemsAsync(ShareOperation shareOperation)
        {
            var bmpItem = await SharedBitmapItem.CreateFromDataPackage(shareOperation.Data);
            if (bmpItem != null)
            {
                this.sharedItems.Add(bmpItem);
            }

            var stgItems = await SharedStorageFileItem.CreateFromDataPackage(shareOperation.Data);
            if (stgItems != null)
            {
                foreach (var stgItem in stgItems)
                {
                    this.sharedItems.Add(stgItem);
                }
            }

            this.SharedItemsView.SelectAll();
        }

        /// <summary>
        ///  共有で渡された登録候補のアイテム.
        /// </summary>
        private System.Collections.ObjectModel.ObservableCollection<ISharedItem> sharedItems = new System.Collections.ObjectModel.ObservableCollection<ISharedItem>();

        /// <summary>
        ///  アイテムの登録情報の変更をキャンセルするための<c>CancellationTokenSource</c>.
        /// </summary>
        private System.Threading.CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        ///  [ロック画面に設定]ボタンのクリックを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SetLockscreenButton_Click(object sender, RoutedEventArgs e)
        {
            var sharedItem = this.SharedItemsView.SelectedItem as ISharedItem;
            if (sharedItem == null)
            {
                return;
            }

            await sharedItem.SetLockscreenAsync();
            App.Current.UpdateLockscreenItemsPageBackgroundAsync();
        }

        #region IDisposable

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

        #endregion
    }
}
