using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Windows.UI.ApplicationSettings;
using Windows.System.UserProfile;

using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;


// アイテム ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234233 を参照してください

namespace MyApps.SetLockscreen
{
    /// <summary>
    /// アイテムのコレクションのプレビューを表示するページです。このページは、分割アプリケーションで使用できる
    /// グループを表示し、その 1 つを選択するために使用されます。
    /// </summary>
    public sealed partial class LockscreenItemsPage : MyApps.SetLockscreen.Common.LayoutAwarePage
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public LockscreenItemsPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        ///  Static Constructor.
        /// </summary>
        static LockscreenItemsPage()
        {
            IsItemChangingProperty = DependencyProperty.Register("IsItemChanging", typeof(bool), typeof(LockscreenItemsPage), null);
            ItemsSelectedProperty = DependencyProperty.Register("ItemsSelected", typeof(bool), typeof(LockscreenItemsPage), null);
        }

        /// <summary>
        ///  アイテム変更(追加or削除)中に<c>true</c>になるプロパティ.
        /// </summary>
        public bool IsItemChanging
        {
            get { return (bool)GetValue(IsItemChangingProperty); }
            set { SetValue(IsItemChangingProperty, value); }
        }

        /// <summary>
        ///  <c>IsItemChanging</c>プロパティ.
        /// </summary>
        public static DependencyProperty IsItemChangingProperty { get; private set; }

        /// <summary>
        ///  アイテムが選択されている時に<c>true</c>になるプロパティ.
        /// </summary>
        public bool ItemsSelected
        {
            get { return (bool)GetValue(ItemsSelectedProperty); }
            set { SetValue(ItemsSelectedProperty, value); }
        }

        /// <summary>
        ///  <c>ItemsSelected</c>プロパティ.
        /// </summary>
        public static DependencyProperty ItemsSelectedProperty { get; private set; }

        /// <summary>ヘルプページのuri文字列</summary>
        const string HelpPageUriString = @"ms-appx-web:///Files/AppHelp.html";

        /// <summary>ヘルプページ(プライバシーポリシー)のuri文字列</summary>
        const string HelpPagePrivacyPolcyUriString = @"ms-appx-web:///Files/AppHelp.html#privacypolicy";

        /// <summary>
        /// このページには、移動中に渡されるコンテンツを設定します。前のセッションからページを
        /// 再作成する場合は、保存状態も指定されます。
        /// </summary>
        /// <param name="navigationParameter">このページが最初に要求されたときに
        /// <see cref="Frame.Navigate(Type, Object)"/> に渡されたパラメーター値。
        /// </param>
        /// <param name="pageState">前のセッションでこのページによって保存された状態の
        /// ディクショナリ。ページに初めてアクセスするとき、状態は null になります。</param>
        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: バインド可能なアイテムのコレクションを this.DefaultViewModel["Items"] に割り当てます
            this.DefaultViewModel["Items"] = App.Current.LockscreenItemList;

            SettingsPane.GetForCurrentView().CommandsRequested += LockscreenItemsPage_CommandsRequested;
            Window.Current.VisibilityChanged += CoreWindow_Current_VisibilityChanged;

            LoadCurrentLockscreenImageAsync();
            await App.Current.LoadAllItemsAsync();

            DoUpdateOnAppLaunchAsync();

            if (App.Current.LockscreenItemList.IsEmpty)
            {
                await Task.Delay(1000);
                ShowAppHelpPopup(new Uri(HelpPageUriString));
            }

            await TileHelper.CleanupUnusedTileIdsAsync();
        }

        /// <summary>
        /// アプリケーションが中断される場合、またはページがナビゲーション キャッシュから破棄される場合、
        /// このページに関連付けられた状態を保存します。値は、
        /// <see cref="SuspensionManager.SessionState"/> のシリアル化の要件に準拠する必要があります。
        /// </summary>
        /// <param name="pageState">シリアル化可能な状態で作成される空のディクショナリ。</param>
        protected override void SaveState(Dictionary<string, object> pageState)
        {
            // 中断->再開とするとOnNavigatedTo()もLoadState()もないので
            // 小細工を弄する.
            this.wasSavedState = true;
        }

        /// <summary>
        ///  <c>SaveState()</c>が呼び出されている場合に<c>true</c>となるフラグ.
        /// </summary>
        private bool wasSavedState = false;

        /// <summary>
        ///  <c>CoreWindow</c>の可視状態が変わった時のイベント処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void CoreWindow_Current_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            // 小細工
            if (this.wasSavedState && e.Visible)
            {
                this.wasSavedState = false;
                LoadCurrentLockscreenImageAsync();
                DoUpdateOnAppLaunchAsync();
                await TileHelper.CleanupUnusedTileIdsAsync();
            }
        }

        #region 設定チャーム

        /// <summary>
        ///  設定チャーム.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void LockscreenItemsPage_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            args.Request.ApplicationCommands.Add(
                new SettingsCommand("UpdateSetting", App.MyResource["SettingCharmUpdateSettingName"], OnSettingCommandUpdateSetting));
            args.Request.ApplicationCommands.Add(
                new SettingsCommand("Help", App.MyResource["SettingCharmHelpName"], OnSettingCommandHelp));
            args.Request.ApplicationCommands.Add(
                new SettingsCommand("PrivacyPolicy", App.MyResource["SettingCharmPrivacyPolicyName"], OnSettingCommandPrivacyPolicy));
            args.Request.ApplicationCommands.Add(
                new SettingsCommand("OpenStore", App.MyResource["SettingCharmOpenStoreName"], OnSettingCommandOpenStore));
        }

        /// <summary>
        ///  ロック画面の更新に関する設定のポップアップを表示する.
        /// </summary>
        /// <param name="command"></param>
        private void OnSettingCommandUpdateSetting(Windows.UI.Popups.IUICommand command)
        {
            if (SettingsPane.Edge == SettingsEdgeLocation.Right)
            {
                this.UpdateSettingPopup.HorizontalOffset = this.ActualWidth;
                this.UpdateSettingPopup.VerticalAlignment = 0;
            }
            else if (SettingsPane.Edge == SettingsEdgeLocation.Left)
            {
                this.UpdateSettingPopup.HorizontalOffset = 0;
                this.UpdateSettingPopup.VerticalAlignment = 0;
                (this.UpdateSettingPopup.ChildTransitions[0] as Windows.UI.Xaml.Media.Animation.EdgeUIThemeTransition).Edge = EdgeTransitionLocation.Left;
            }

            (this.UpdateSettingPopup.Child as UserControl).Height = this.ActualHeight;
            this.UpdateSettingPopup.IsOpen = true;
        }

        /// <summary>
        ///  ロック画面の更新に関する設定のポップアップのLoadedイベントを処理する.
        ///  (チャームが右にある場合にポップアップの表示位置を調整する)
        /// </summary>
        private void UpdateSettingPopup_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.UpdateSettingPopup.HorizontalOffset == this.ActualWidth)
            {
                this.UpdateSettingPopup.HorizontalOffset -= (this.UpdateSettingPopup.Child as Control).ActualWidth;
            }
        }

        /// <summary>
        ///  ロック画面の更新に関する設定のポップアップのUnloadedイベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateSettingPopup_Completed(object sender, EventArgs e)
        {
            // task登録
            App.Current.UpdateBackgroundTaskRegistration();
        }

        /// <summary>
        ///  [設定チャーム]->[Help].
        /// </summary>
        /// <param name="command"></param>
        private void OnSettingCommandHelp(Windows.UI.Popups.IUICommand command)
        {
            ShowAppHelpPopup(new Uri(HelpPageUriString));
        }

        /// <summary>
        ///  [設定チャーム]->[Privacy Policy].
        /// </summary>
        /// <param name="command"></param>
        private void OnSettingCommandPrivacyPolicy(Windows.UI.Popups.IUICommand command)
        {
            ShowAppHelpPopup(new Uri(HelpPagePrivacyPolcyUriString));
        }

        /// <summary>
        ///  ヘルプのポップアップを表示する
        /// </summary>
        /// <param name="helpUri"></param>
        private void ShowAppHelpPopup(Uri helpUri)
        {
            var border = (this.HelpPrivacyPolicyPopup.Child as Border);
            border.Width = this.ActualWidth * 0.7;
            border.Height = this.ActualHeight * 0.7;

            this.HelpPageWebView.Navigate(helpUri);

            this.HelpPrivacyPolicyPopup.HorizontalOffset = this.ActualWidth * 0.15;
            this.HelpPrivacyPolicyPopup.VerticalOffset = this.ActualHeight * 0.15;
            this.HelpPrivacyPolicyPopup.IsOpen = true;
        }

        /// <summary>
        ///  ヘルプのポップアップの<c>Loaded</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpPrivacyPolicyPopup_Loaded(object sender, RoutedEventArgs e)
        {
            this.HelpPageWebView.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        /// <summary>
        ///  ヘルプのポップアップの[閉じる]ボタン.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppHelpCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.HelpPrivacyPolicyPopup.IsOpen = false;
        }

        /// <summary>
        ///  [設定チャーム]->[Store].
        /// </summary>
        /// <param name="command"></param>
        private async void OnSettingCommandOpenStore(Windows.UI.Popups.IUICommand command)
        {
            Uri uri = new Uri(@"ms-windows-store:PDP?PFN=17787zakii.64709012E2351_pd7rzaxsjxxq8");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        #endregion

        /// <summary>
        ///  アプリ起動時のロック画面切り替えの処理を行う.
        /// </summary>
        private async void DoUpdateOnAppLaunchAsync()
        {
            // セカンダリタイル起動のロック画面切り替えと重複しないようにする
            if (App.Current.LaunchedFromSecondaryTile)
            {
                return;
            }

            if (App.Current.LockscreenItemList.IsEmpty)
            {
                return;
            }

            var trigger = AppSettings.Instance.GetUpdateTriggerOnAppLaunch();
            if (!trigger.IsOn)
            {
                return;
            }

            if (SetLockscreenCore.IsAutoSetSuppressedNow())
            {
                return;
            }

            if (trigger.ShouldAskUser)
            {
                var msg = new Windows.UI.Popups.MessageDialog(App.MyResource["UpdateOnAppLaunchMessage"], App.MyResource["UpdateOnAppLaunchTitle"]);
                msg.Commands.Add(new Windows.UI.Popups.UICommand(App.MyResource["UpdateOnAppLaunchCommandSet"]));
                msg.Commands.Add(new Windows.UI.Popups.UICommand(App.MyResource["UpdateOnAppLaunchCommandNotSet"]));
                msg.DefaultCommandIndex = 0;
                msg.CancelCommandIndex = 1;
                var cmd = await msg.ShowAsync();
                if (cmd != msg.Commands[0])
                {
                    return;
                }
            }

            var setLockscreen = new SetLockscreenCore();
            await setLockscreen.SetNextItemToLockscreenAsync();
            LoadCurrentLockscreenImageAsync();
        }

        /// <summary>
        ///  バックグラウンドタスクが実行完了したときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public async void BackgroundTask_Completed(Windows.ApplicationModel.Background.BackgroundTaskRegistration sender, Windows.ApplicationModel.Background.BackgroundTaskCompletedEventArgs args)
        {
            // バックグラウンドタスク完了イベントはワーカースレッドで通知されるので
            // UI threadで実行する必要あり
            await this.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Low,
                () => { LoadCurrentLockscreenImageAsync(); });
        }

        /// <summary>
        ///  セカンダリタイルから起動された時の処理.
        /// </summary>
        /// <param name="tileId"></param>
        public async void SecondaryTile_LaunchedAsync(string tileId)
        {
            var secondaryTile = await TileHelper.GetSecondaryTileFromTileIdAsync(tileId);
            if (secondaryTile == null)
            {
                App.Current.LaunchedFromSecondaryTile = false;
                return;
            }

            await App.Current.LoadAllItemsAsync();
            var item = App.Current.LockscreenItemList.FirstOrDefault(x => tileId.Equals(x.Setting.TileId, StringComparison.Ordinal));
            if (item == null)
            {
                // delete from start
                if (await secondaryTile.RequestDeleteAsync())
                {
                    await TileHelper.RemoveTileRegistrationAsync(tileId);
                }
                App.Current.LaunchedFromSecondaryTile = false;
                return;
            }
            else
            {
                var succeedToSet = await SetSelectedItemToLockscreenAsync(item);
                LoadCurrentLockscreenImageAsync();
                App.Current.LaunchedFromSecondaryTile = false;
            }
        }

        /// <summary>
        ///  現在のロック画面に設定されているイメージをアプリの背景に設定する.
        /// </summary>
        /// <remarks>
        ///  このアプリでロック画面を設定していない場合、何も取得できない可能性がある.
        ///  (MSDNのAPI仕様通り)
        /// </remarks>
        public async void LoadCurrentLockscreenImageAsync()
        {
            using (var stream = LockScreen.GetImageStream())
            {
                if (stream == null)
                {
                    return;
                }

                var image = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                await image.SetSourceAsync(stream);

                this.BackgroundBrush.ImageSource = image;
            }
        }

        /// <summary>
        ///  アイテムの登録情報の変更をキャンセルするための<c>CancellationTokenSource</c>.
        /// </summary>
        private System.Threading.CancellationTokenSource cancellationTokenSource;

        /// <summary>
        ///  アイテムの登録情報の変更をキャンセルする.
        /// </summary>
        private void CancelChangingItems()
        {
            if ((this.cancellationTokenSource != null) && !this.cancellationTokenSource.IsCancellationRequested)
            {
                this.cancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        ///  ロック画面に設定するアイテムをFilePickerで選択して登録する.
        /// </summary>
        private async void PickAndAddItemsAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.CommitButtonText = App.MyResource["FilePickerOpenCommitButtonText"];

            var files = await picker.PickMultipleFilesAsync();
            if (files == null)
            {
                return;
            }

            bool emptyBefore = App.Current.LockscreenItemList.IsEmpty;
            this.IsItemChanging = true;
            List<StorageFile> failedFiles = new List<StorageFile>();

            using (this.cancellationTokenSource = new System.Threading.CancellationTokenSource())
            {
                try
                {
                    foreach (var file in files)
                    {
                        // 画像ファイル以外を排除
                        var properties = await file.Properties.GetImagePropertiesAsync();
                        if ((properties.Width == 0) || (properties.Height == 0))
                        {
                            continue;
                        }

                        if (!await App.Current.AddFileAsync(file, false, this.cancellationTokenSource.Token))
                        {
                            failedFiles.Add(file);
                        }
                    }

                    if (failedFiles.Count != 0)
                    {
                        var msg = String.Format(App.MyResource["ErrorMessageAddFiles"], failedFiles.Count);
                        var dlg = new Windows.UI.Popups.MessageDialog(msg);
                        await dlg.ShowAsync();
                    }
                }
                catch (TaskCanceledException ex)
                {
                    // 知らないキャンセルはCrashさせる
                    if (ex.CancellationToken != this.cancellationTokenSource.Token)
                    {
                        throw;
                    }
                }
            }
            this.cancellationTokenSource = null;

            this.IsItemChanging = false;

            // empty -> not empty
            //   updatereg
            if (emptyBefore && !App.Current.LockscreenItemList.IsEmpty)
            {
                App.Current.UpdateBackgroundTaskRegistration();
            }
        }

        /// <summary>
        ///  選択されているアイテムをロック画面設定対象から登録解除する.
        /// </summary>
        private async void RemoveSelectedItemsAsync()
        {
            bool emptyBefore = App.Current.LockscreenItemList.IsEmpty;
            this.IsItemChanging = true;

            var itemsView = GetItemsView();
            if (itemsView == null)
            {
                return;
            }

            var removedItems = new List<LockscreenImageItem>(itemsView.SelectedItems.Count);
            removedItems.AddRange(itemsView.SelectedItems.Cast<LockscreenImageItem>());
            foreach (var item in removedItems)
            {
//                await this.m_lockscreenItemList.RemoveAsync(item);
                await App.Current.LockscreenItemList.RemoveAsync(item);
            }

            this.IsItemChanging = false;

            // not empty -> empty
            //   updatereg
//            if (!emptyBefore && this.m_lockscreenItemList.IsEmpty)
            if (!emptyBefore && App.Current.LockscreenItemList.IsEmpty)
            {
                App.Current.UpdateBackgroundTaskRegistration();
            }
        }

        /// <summary>
        ///  選択されているアイテムをロック画面に設定する.
        /// </summary>
        private async void SetSelectedItemToLockscreenAsync()
        {
            var itemsView = GetItemsView();
            if (itemsView == null)
            {
                return;
            }
            var item = itemsView.SelectedItem as LockscreenImageItem;
            if (item == null)
            {
                return;
            }

            bool succeedtoSet = await SetSelectedItemToLockscreenAsync(item);

            AppSettings.Instance.CurrentIndex = itemsView.SelectedIndex;
            LoadCurrentLockscreenImageAsync();
        }

        /// <summary>
        ///  アイテムをロック画面に設定する.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private async Task<bool> SetSelectedItemToLockscreenAsync(LockscreenImageItem item)
        {
            bool succeedToSet = false;
            try
            {
                await LockScreen.SetImageFileAsync(item.Item);
                succeedToSet = true;
            }
            catch (UnauthorizedAccessException)
            {
                // 画像ファイルを貼り付けしてその直後に
                // ロック画面に設定しようとすると↑はアクセス拒否で失敗する。
                // その場合でも↓で問題ないのでそれを試す。
                // もしかしたら常に↓でよいのかもしれない...
            }

            if (!succeedToSet)
            {
                using (var strm = await item.Item.OpenReadAsync())
                {
                    await LockScreen.SetImageStreamAsync(strm);
                    succeedToSet = true;
                }
            }

            return succeedToSet;
        }


        /// <summary>
        ///  選択されているアイテムをスタート画面にピン留めする.
        /// </summary>
        /// <param name="buttonPosition">呼び出し元ボタンの場所</param>
        private async void PinSelectedItemToStartAsync(Point buttonPosition)
        {
            var item = GetSelectedItem();
            if (item == null)
            {
                return;
            }

            var tile = await Tile.CreateSecodaryTileForItem(item);
            if (tile == null)
            {
                return;
            }

            tile.SecondaryTile.DisplayName = String.Format("{0} - {1}", App.MyResource["AppName"], item.Name);
            var re = new Rect(buttonPosition.X, buttonPosition.Y, 100, 100);
            if (await tile.SecondaryTile.RequestCreateForSelectionAsync(re))
            {
                await TileHelper.CleanupUnusedTileIdsAsync();

                item.Setting.TileId = tile.SecondaryTile.TileId;
                App.Current.LockscreenItemList.SaveItemsToSetting();
                TileHelper.SaveTileRegistration(tile);
                UpdatePinUnPinToStartBottomAppBarButtons();
            }
            else
            {
                await tile.DeleteTileImageCacheFilesAsync();
            }
        }

        /// <summary>
        ///  選択されているアイテムをスタート画面からピン留めを外す.
        /// </summary>
        /// <param name="buttonPosition">呼び出し元ボタンの場所</param>
        private async void UnPinSelectedItemToStartAsync(Point buttonPosition)
        {
            var item = GetSelectedItem();
            if (item == null)
            {
                return;
            }

            var tile = await Tile.GetSecondaryTileForItem(item);
            if (tile == null)
            {
                if (!String.IsNullOrEmpty(item.Setting.TileId))
                {
                    await TileHelper.RemoveTileRegistrationAsync(item.Setting.TileId);
                    item.Setting.TileId = null;
                }
                UpdatePinUnPinToStartBottomAppBarButtons();
                return;
            }

            string tileId = tile.SecondaryTile.TileId;
            var re = new Rect(buttonPosition.X, buttonPosition.Y, 100, 100);
            if (await tile.SecondaryTile.RequestDeleteForSelectionAsync(re))
            {
                item.Setting.TileId = null;
                App.Current.LockscreenItemList.SaveItemsToSetting();
                await TileHelper.RemoveTileRegistrationAsync(tileId);
                UpdatePinUnPinToStartBottomAppBarButtons();
            }
        }

        /// <summary>
        ///  アイテム一覧の<c>GridView</c>または<c>ListView</c>の選択されているアイテムが変わったイベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemsView = GetItemsView();
            if (sender != itemsView)
            {
                return;
            }

            this.ItemsSelected = (itemsView.SelectedItem != null);
        }

        /// <summary>
        ///  現在表示されている<c>GridView</c>か<c>ListView</c>を取得する.
        /// </summary>
        /// <returns></returns>
        private ListViewBase GetItemsView()
        {
            if (this.itemGridView.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                return this.itemGridView;
            }
            else if (this.itemListView.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                return this.itemListView;
            }

            return null;
        }

        /// <summary>
        ///  現在選択されているアイテムを取得する.
        /// </summary>
        /// <returns></returns>
        private LockscreenImageItem GetSelectedItem()
        {
            var itemsView = GetItemsView();
            if (itemListView == null)
            {
                return null;
            }

            var item = itemsView.SelectedItem as LockscreenImageItem;
            if (item == null)
            {
                return null;
            }

            return item;            
        }

        /// <summary>
        ///  アイテム登録情報の変更をキャンセルするボタンの<c>Click</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelItemChangingButton_Click(object sender, RoutedEventArgs e)
        {
            CancelChangingItems();
        }

        #region AppBar

        /// <summary>
        ///  AppBarの<c>Loaded</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BottomAppBar_Loaded(object sender, RoutedEventArgs e)
        {
            var itemsView = GetItemsView();
            this.ItemsSelected = (itemListView != null) ? (itemsView.SelectedItem != null) : false;

            (sender as AppBar).DataContext = this;

            var panel = (sender as AppBar).Content as Panel;
            foreach (var ui in panel.Children)
            {
                base.StartLayoutUpdates(ui, new RoutedEventArgs());
            }

            PreparePastePopupContentsAsync();

            await TileHelper.CleanupUnusedTileIdsAsync();
            UpdatePinUnPinToStartBottomAppBarButtons();
        }

        /// <summary>
        ///  AppBarの<c>Unloaded</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BottomAppBar_Unloaded(object sender, RoutedEventArgs e)
        {
            var panel = (sender as AppBar).Content as Panel;
            foreach (var ui in panel.Children)
            {
                base.StopLayoutUpdates(ui, new RoutedEventArgs());
            }
        }

        /// <summary>
        ///  pin / unpinのアプリバーボタンの表示状態を更新する.
        /// </summary>
        private void UpdatePinUnPinToStartBottomAppBarButtons()
        {
            var item = GetSelectedItem();
            if (item != null)
            {
                if (!TileHelper.IsTileRegistered(item.Setting.TileId))
                {
                    this.PinToStartAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    this.UnPinFromStartAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                else
                {
                    this.PinToStartAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    this.UnPinFromStartAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
            }
            else
            {
                this.PinToStartAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                this.UnPinFromStartAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        /// <summary>
        ///  登録追加ボタン.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            PickAndAddItemsAsync();
        }

        /// <summary>
        ///  登録解除ボタン.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedItemsAsync();
        }

        /// <summary>
        ///  ロック画面に設定ボタン.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetLockscreenButton_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedItemToLockscreenAsync();
        }

        /// <summary>
        ///  スタート画面にピン留めボタン.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PinToStartButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var t = button.TransformToVisual(this);
            var p = t.TransformPoint(new Point(button.ActualWidth / 2.0, button.ActualHeight / 2.0));

            PinSelectedItemToStartAsync(p);
        }

        /// <summary>
        ///  スタート画面からピン留めを外すボタン.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnPinToStartButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var t = button.TransformToVisual(this);
            var p = t.TransformPoint(new Point(button.ActualWidth / 2.0, button.ActualHeight / 2.0));

            UnPinSelectedItemToStartAsync(p);
        }

        /// <summary>
        ///  クリップボードから貼り付けボタン.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PasteAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var t = button.TransformToVisual(this);
            var p = t.TransformPoint(new Point(button.ActualWidth/2.0, button.ActualHeight/2.0));

            ShowPastePopupAsync(p);
        }

        #endregion

        /// <summary>
        ///  貼り付けのポップアップの中身をクリップボードを参照して設定する.
        /// </summary>
        private async void PreparePastePopupContentsAsync()
        {
            var dataPackageView = Clipboard.GetContent();
            var sharedItems = new List<ISharedItem>();

            var bmpItem = await SharedBitmapItem.CreateFromDataPackage(dataPackageView);
            if (bmpItem != null)
            {
                sharedItems.Add(bmpItem);
            }

            var stgItems = await SharedStorageFileItem.CreateFromDataPackage(dataPackageView);
            if (stgItems != null)
            {
                foreach (var stgItem in stgItems)
                {
                    sharedItems.Add(stgItem);
                }
            }
            if (sharedItems.Count == 0)
            {
                this.PasteAppBarButton.IsEnabled = false;
                return;
            }

            this.pastedItemsView.ItemsSource = sharedItems;
            this.pastedItemsView.SelectAll();
            this.PasteAppBarButton.IsEnabled = true;
        }

        /// <summary>
        ///  貼り付けのポップアップを表示する
        /// </summary>
        /// <param name="buttonPosition">貼り付けポップアップ表示のボタンの場所</param>
        private void ShowPastePopupAsync(Point buttonPosition)
        {
            this.PastePopup.HorizontalOffset = buttonPosition.X;
            this.PastePopup.VerticalOffset = this.ActualHeight - this.BottomAppBar.ActualHeight;
            this.PastePopup.IsOpen = true;
        }

        /// <summary>
        ///  貼り付けポップアップの<c>Loaded</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PastePopup_Border_Loaded(object sender, RoutedEventArgs e)
        {
            // adjust location
            var popup = (sender as Border).Parent as Popup;
            popup.HorizontalOffset -= (sender as Border).ActualWidth / 2.0;
            popup.VerticalOffset -= (sender as Border).ActualHeight;
        }

        /// <summary>
        ///  貼り付けポップアップの貼り付けボタンがクリックされたときの処理を行う.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PastePopup_PasteButton_Click(object sender, RoutedEventArgs e)
        {
            using (this.cancellationTokenSource = new System.Threading.CancellationTokenSource())
            {
                try
                {
                    bool emptyBefore = App.Current.LockscreenItemList.IsEmpty;

                    foreach (var item in this.pastedItemsView.SelectedItems)
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
            }
        }
    }
}
