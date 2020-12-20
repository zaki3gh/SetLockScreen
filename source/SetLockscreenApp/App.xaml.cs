using MyApps.SetLockscreen.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

// 分割アプリケーション テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234228 を参照してください

namespace MyApps.SetLockscreen
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
        /// 最初の行であり、main() または WinMain() と論理的に等価です。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// アプリケーションがエンド ユーザーによって正常に起動されたときに呼び出されます。他のエントリ ポイントは、
        /// アプリケーションが特定のファイルを開くために呼び出されたときに
        /// 検索結果やその他の情報を表示するために使用されます。
        /// </summary>
        /// <param name="args">起動要求とプロセスの詳細を表示します。</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // セカンダリタイルからの起動
            if (String.Equals(TileHelper.TileArgumentsSetLockscreen, args.Arguments, StringComparison.Ordinal))
            {
                this.LaunchedFromSecondaryTile = true;
            }

            // ウィンドウに既にコンテンツが表示されている場合は、アプリケーションの初期化を繰り返さずに、
            // ウィンドウがアクティブであることだけを確認してください

            if (rootFrame == null)
            {
                // ナビゲーション コンテキストとして動作するフレームを作成し、最初のページに移動します
                rootFrame = new Frame();
                //フレームを SuspensionManager キーに関連付けます                                
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // 必要な場合のみ、保存されたセッション状態を復元します
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //状態の復元に何か問題があります。
                        //状態がないものとして続行します
                    }
                }

                // フレームを現在のウィンドウに配置します
                Window.Current.Content = rootFrame;

                this.OnLaunchedDispatcher = rootFrame.Dispatcher;
            }
            if (rootFrame.Content == null)
            {
                // ナビゲーション スタックが復元されていない場合、最初のページに移動します。
                // このとき、必要な情報をナビゲーション パラメーターとして渡して、新しいページを
                // を構成します
                if (!rootFrame.Navigate(typeof(LockscreenItemsPage), "AllGroups"))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // 現在のウィンドウがアクティブであることを確認します
            Window.Current.Activate();

            if (this.LaunchedFromSecondaryTile)
            {
                var appPage = rootFrame.Content as LockscreenItemsPage;
                if (appPage != null)
                {
                    appPage.SecondaryTile_LaunchedAsync(args.TileId);
                }
            }
        }

        /// <summary>
        /// アプリケーションの実行が中断されたときに呼び出されます。アプリケーションの状態は、
        /// アプリケーションが終了されるのか、メモリの内容がそのままで再開されるのか
        /// わからない状態で保存されます。
        /// </summary>
        /// <param name="sender">中断要求の送信元。</param>
        /// <param name="e">中断要求の詳細。</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        /// <summary>
        /// 共有操作のターゲットとしてアプリケーションがアクティブにされたときに呼び出されます。
        /// </summary>
        /// <param name="args">アクティベーション要求の詳細。</param>
        protected override void OnShareTargetActivated(Windows.ApplicationModel.Activation.ShareTargetActivatedEventArgs args)
        {
            var shareTargetPage = new ShareTargetPage();
            shareTargetPage.Activate(args);
        }

        /// <summary>
        ///  セカンダリタイルから起動されたかどうか.
        /// </summary>
        internal bool LaunchedFromSecondaryTile { get; set; }

        /// <summary>
        ///  メイン画面(<c>LockscreenItemsPage</c>)に関連付けられている<c>Dispatcher</c>.
        /// </summary>
        private Windows.UI.Core.CoreDispatcher OnLaunchedDispatcher{ get; set; }

        /// <summary>
        ///  ロック画面に登録するアイテムのリスト
        /// </summary>
        public LockscreenItemList LockscreenItemList
        {
            get { return this.m_lockscreenItemList; }
        }

        /// <summary>
        ///  <c>LockscreenItemList</c>プロパティ.
        /// </summary>
        private LockscreenItemList m_lockscreenItemList = new LockscreenItemList();

        /// <summary>
        ///  登録済みの全アイテムを読み込む.
        /// </summary>
        /// <returns></returns>
        public Task LoadAllItemsAsync()
        {
            return this.m_lockscreenItemList.LoadAllItemsAsync();
        }

        /// <summary>
        ///  ファイルを追加する.
        /// </summary>
        /// <param name="file">追加するファイル</param>
        /// <param name="cancellationToken">キャンセル用のトークン</param>
        /// <returns></returns>
        public async Task<bool> AddFileAsync(StorageFile file, bool isTemporaryDefault, CancellationToken cancellationToken)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var item = await LockscreenImageItemSettingHelper.FromStorageItemAsync(file, isTemporaryDefault, cancellationToken);
            if (item == null)
            {
                return false;
            }

            if ((this.OnLaunchedDispatcher == null) || this.OnLaunchedDispatcher.HasThreadAccess)
            {
                await this.LockscreenItemList.AddItemAsync(item);
            }
            else
            {
                await this.OnLaunchedDispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal,
                    async () => { await this.LockscreenItemList.AddItemAsync(item); });
            }

            return true;
        }

        /// <summary>
        ///  バックグラウンドタスクの登録状況を更新する.
        /// </summary>
        public void UpdateBackgroundTaskRegistration()
        {
            var maintenance = AppSettings.Instance.GetUpdateTriggerOnMaintenance();
            var networkStateChange = AppSettings.Instance.GetUpdateTriggerOnNetworkStateChange();

            // アイテムがひとつも登録されていない場合
            // タスクを実行しても無駄なのでoffとして登録する
            // この変更は当然AppSettingsには保存しない
            if (this.LockscreenItemList.IsEmpty)
            {
                maintenance.IsOn = false;
                networkStateChange.IsOn = false;
            }

            var taskRegM = maintenance.UpdateBackgroundTaskRegistration();
            var taskRegN = networkStateChange.UpdateBackgroundTaskRegistration();

            if (taskRegM != null)
            {
                taskRegM.Completed += BackgroundTask_Completed;
            }
            if (taskRegN != null)
            {
                taskRegN.Completed += BackgroundTask_Completed;
            }
        }

        /// <summary>
        ///  バックグラウンドタスクが実行完了したときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void BackgroundTask_Completed(Windows.ApplicationModel.Background.BackgroundTaskRegistration sender, Windows.ApplicationModel.Background.BackgroundTaskCompletedEventArgs args)
        {
            // バックグラウンドタスク完了イベントはワーカースレッドで通知されるので
            // UI threadで実行する必要あり

            if (this.OnLaunchedDispatcher == null)
            {
                return;
            }

            await this.OnLaunchedDispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => 
                {
                    if (Window.Current == null)
                    {
                        return;
                    }
                    var frame = Window.Current.Content as Frame;
                    if (frame == null)
                    {
                        return;
                    }
                    var page = frame.Content as LockscreenItemsPage;
                    if (page == null)
                    {
                        return;
                    }
                    page.BackgroundTask_Completed(sender, args);
                });
        }

        /// <summary>
        ///  メイン画面の背景を更新する.
        /// </summary>
        public async void UpdateLockscreenItemsPageBackgroundAsync()
        {
            if (this.OnLaunchedDispatcher == null)
            {
                return;
            }

            await this.OnLaunchedDispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    if (Window.Current == null)
                    {
                        return;
                    }
                    var frame = Window.Current.Content as Frame;
                    if (frame == null)
                    {
                        return;
                    }
                    var page = frame.Content as LockscreenItemsPage;
                    if (page == null)
                    {
                        return;
                    }
                    page.LoadCurrentLockscreenImageAsync();
                });

        }

        /// <summary>
        ///  このアプリの現在のインスタンス.
        /// </summary>
        static internal new App Current
        {
            get { return Application.Current as App; }
        }

        /// <summary>
        ///  このアプリの文字列リソース.
        /// </summary>
        static internal MyStringResources MyResource
        {
            get { return s_myResource; }
        }

        /// <summary>MyResourceプロパティ</summary>
        static MyStringResources s_myResource = new MyStringResources();
    }
}
