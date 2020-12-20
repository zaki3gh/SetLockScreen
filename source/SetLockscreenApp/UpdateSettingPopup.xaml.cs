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

// ユーザー コントロールのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  ロック画面の切り替えに関する設定ポップアップ(の中身).
    /// </summary>
    public sealed partial class UpdateSettingPopup : UserControl
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public UpdateSettingPopup()
        {
            this.InitializeComponent();
        }

        /// <summary>
        ///  <c>Loaded</c>イベントのハンドラー.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void This_Loaded(object sender, RoutedEventArgs e)
        {
            this.AppLaunch = AppSettings.Instance.GetUpdateTriggerOnAppLaunch();
            this.AppLaunchSetting.DataContext = this.AppLaunch;

            this.Maintenance = AppSettings.Instance.GetUpdateTriggerOnMaintenance();
            this.MaintenanceSetting.DataContext = this.Maintenance;

            this.NetworkStateChange = AppSettings.Instance.GetUpdateTriggerOnNetworkStateChange();
            this.NetworkStateChangeSetting.DataContext = this.NetworkStateChange;

            this.Miscellaneous = AppSettings.Instance.GetMiscellaneousSetting();
            this.MiscellaneousSetting.DataContext = this.Miscellaneous;
        }

        /// <summary>
        ///  <c>Unloaded</c>イベントのハンドラー.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void This_Unloaded(object sender, RoutedEventArgs e)
        {
            AppSettings.Instance.SetUpdateTriggerOnAppLaunch(this.AppLaunch);
            AppSettings.Instance.SetUpdateTriggerOnMaintenance(this.Maintenance);
            AppSettings.Instance.SetUpdateTriggerOnNetworkStateChange(this.NetworkStateChange);
            AppSettings.Instance.SetMiscellaneousSetting(this.Miscellaneous);

            OnCompleted();
        }

        /// <summary>
        ///  「戻る」ボタンがクリックされた.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var popup = this.Parent as Popup;
            if (popup != null)
            {
                popup.IsOpen = false;
            }

            // If the app is not snapped, then the back button shows the Settings pane again.
            if (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped)
            {
                Windows.UI.ApplicationSettings.SettingsPane.Show();
            }
        }

        /// <summary>
        ///  設定変更が完了したときのイベント.
        /// </summary>
        public event EventHandler Completed;

        /// <summary>
        ///  <c>Completed</c>イベントを発生させる.
        /// </summary>
        private void OnCompleted()
        {
            var handler = this.Completed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>アプリ起動時の切り替え設定</summary>
        public UpdateTriggerOnAppLaunch AppLaunch { get; private set; }

        /// <summary>一定時間ごとの切り替え設定</summary>
        public UpdateTriggerOnMaintenance Maintenance { get; private set; }

        /// <summary>ネットワークの状態変更時の切り替え設定</summary>
        public UpdateTriggerOnNetworkStateChange NetworkStateChange { get; private set; }

        /// <summary>切り替えの最小間隔の設定</summary>
        public MiscellaneousSetting Miscellaneous { get; private set; }
    }

    /// <summary>
    ///  更新間隔(分)用の<c>ValueConverter</c>実装(<c>uint</c>またはスライダーの<c>double</c>)
    /// </summary>
    class FreshnessTimeValueConverter : IValueConverter
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public FreshnessTimeValueConverter()
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                this.minutesFormat = "{0}分";
                this.hoursMinutesFormat = "{0}時間{1}分";
            }
            else
            {
                this.minutesFormat = App.MyResource["MinutesFormat"];
                this.hoursMinutesFormat = App.MyResource["HoursMinutesFormat"];
            }
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            uint? minute = value is double ? (uint)(double)value : value as uint?;
            if (!minute.HasValue)
            {
                return null;
            }

            var hours = (uint)minute / 60u;
            var minutes = (uint)minute % 60u;

            if (hours == 0)
            {
                return String.Format(this.minutesFormat, minutes);
            }
            else
            {
                return String.Format(this.hoursMinutesFormat, hours, minutes);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        /// <summary>1時間未満の場合に使う書式</summary>
        private string minutesFormat;

        /// <summary>1時間以上の場合に使う書式</summary>
        private string hoursMinutesFormat;
    }
}
