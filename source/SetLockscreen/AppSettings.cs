using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;
using Windows.Storage;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  アプリケーションの設定.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        ///  唯一のインスタンス.
        /// </summary>
        public static AppSettings Instance
        {
            get { return s_instance; }
        }

        /// <summary><c>Instance</c>プロパティ</summary>
        static AppSettings s_instance = new AppSettings();

        /// <summary>
        ///  Constructor.
        /// </summary>
        private AppSettings()
        {
            this.CurrentIndex = GetSetting(SettingKey.CurrentIndexOfItem, 0);
        }

        /// <summary>
        ///  設定値を取得する.
        /// </summary>
        /// <typeparam name="T">設定値の型</typeparam>
        /// <param name="key">設定のキー</param>
        /// <param name="defValue">既定値</param>
        /// <returns>設定値</returns>
        private T GetSetting<T>(SettingKey key, T defValue)
        {
            return ApplicationData.Current.LocalSettings.GetValue(key.ToString(), defValue);
        }

        /// <summary>
        ///  設定値を設定する.
        /// </summary>
        /// <typeparam name="T">設定値の型</typeparam>
        /// <param name="key">設定のキー</param>
        /// <param name="value">設定値</param>
        private void SetSetting<T>(SettingKey key, T value)
        {
            ApplicationData.Current.LocalSettings.Values[key.ToString()] = value;
        }

        /// <summary>
        ///  設定値を削除する.
        /// </summary>
        /// <param name="key">設定のキー</param>
        private void RemoveSetting(SettingKey key)
        {
            ApplicationData.Current.LocalSettings.Values.Remove(key.ToString());
        }

        #region Item Setting

        /// <summary>ロック画面に設定するアイテムの登録数上限のキー</summary>
        public int MaxNumberOfItems
        {
            get
            {
                var trialMode = new TrialMode();
                return GetSetting(SettingKey.MaxNumberOfItems, trialMode.MaxNumberOfItems); 
            }
        }

        /// <summary>
        ///  ロック画面に設定するアイテムの登録数.
        /// </summary>
        public int NumberOfItems
        {
            get { return GetSetting(SettingKey.NumberOfItems, 0); }
            private set
            {
                SetSetting(SettingKey.NumberOfItems, value);
                if (value == 0)
                {
                    this.CurrentIndex = -1;
                }
            }
        }

        /// <summary>
        ///  今ロック画面に設定されている(はずの)アイテムのインデックス.
        /// </summary>
        public int CurrentIndex
        {
            get { return GetSetting(SettingKey.CurrentIndexOfItem, -1); }
            set { SetSetting(SettingKey.CurrentIndexOfItem, value); }
        }

        /// <summary>
        ///  ロック画面に設定するアイテムの登録情報を取得する.
        /// </summary>
        /// <returns>ロック画面に設定するアイテムの登録情報</returns>
        public IDictionary<string, object> GetItemsState()
        {
            return GetSetting<ApplicationDataCompositeValue>(SettingKey.Items, null);
        }

        /// <summary>
        ///  ロック画面に設定するアイテムの登録情報を設定する.
        /// </summary>
        /// <param name="items">ロック画面に設定するアイテムの登録情報</param>
        public void SetItems(IEnumerable<LockscreenImageItem> items)
        {
            var itemsState = new ApplicationDataCompositeValue();
            items.SerializeItems(itemsState);

            SetSetting(SettingKey.Items, itemsState);
            this.NumberOfItems = itemsState.Count;
        }

        /// <summary>
        ///  ロック画面を設定した時刻.
        /// </summary>
        public DateTimeOffset TimeOfSetLockscreen
        {
            get { return GetSetting(SettingKey.TimeOfSetLockscreen, DateTimeOffset.MinValue); }
            set { SetSetting(SettingKey.TimeOfSetLockscreen, value); }
        }

        #endregion

        #region UpdateTrigger Setting

        /// <summary>
        ///  ロック画面切り替えに関する設定値を取得する.
        /// </summary>
        /// <param name="key">設定のキー</param>
        /// <param name="trigger">設定値</param>
        private void GetSettingOfUpdateTrigger(SettingKey key, UpdateTriggerBase trigger)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException("trigger");
            }

            var state = GetSetting<ApplicationDataCompositeValue>(key, null);
            if (state != null)
            {
                trigger.LoadFrom(state);
            }
        }

        /// <summary>
        ///  ロック画面切り替えに関する設定値を設定する.
        /// </summary>
        /// <param name="key">設定のキー</param>
        /// <param name="trigger">設定値</param>
        private void SetSettingOfUpdateTrigger(SettingKey key, UpdateTriggerBase trigger)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException("trigger");
            }

            var state = new ApplicationDataCompositeValue();
            trigger.SaveTo(state);
            SetSetting(key, state);
        }

        /// <summary>
        ///  get <c>UpdateTriggerOnAppLaunch</c>.
        /// </summary>
        /// <returns></returns>
        public UpdateTriggerOnAppLaunch GetUpdateTriggerOnAppLaunch()
        {
            var trigger = new UpdateTriggerOnAppLaunch();
            GetSettingOfUpdateTrigger(SettingKey.UpdateTriggerOnAppLaunch, trigger);
            return trigger;
        }

        /// <summary>
        ///  set <c>UpdateTriggerOnAppLaunch</c>.
        /// </summary>
        /// <param name="trigger"></param>
        public void SetUpdateTriggerOnAppLaunch(UpdateTriggerOnAppLaunch trigger)
        {
            SetSettingOfUpdateTrigger(SettingKey.UpdateTriggerOnAppLaunch, trigger);
        }

        /// <summary>
        ///  get <c>UpdateTriggerOnMaintenance</c>.
        /// </summary>
        /// <returns></returns>
        public UpdateTriggerOnMaintenance GetUpdateTriggerOnMaintenance()
        {
            var trigger = new UpdateTriggerOnMaintenance();
            GetSettingOfUpdateTrigger(SettingKey.UpdateTriggerOnMainenance, trigger);
            return trigger;
        }

        /// <summary>
        ///  set <c>UpdateTriggerOnMaintenance</c>.
        /// </summary>
        /// <param name="trigger"></param>
        public void SetUpdateTriggerOnMaintenance(UpdateTriggerOnMaintenance trigger)
        {
            SetSettingOfUpdateTrigger(SettingKey.UpdateTriggerOnMainenance, trigger);
        }

        /// <summary>
        ///  get <c>UpdateTriggerOnNetworkStateChange</c>.
        /// </summary>
        /// <returns></returns>
        public UpdateTriggerOnNetworkStateChange GetUpdateTriggerOnNetworkStateChange()
        {
            var trigger = new UpdateTriggerOnNetworkStateChange();
            GetSettingOfUpdateTrigger(SettingKey.UpdateTriggerOnNetworkStateChange, trigger);
            return trigger;
        }

        /// <summary>
        ///  set <c>UpdateTriggerOnNetworkStateChange</c>.
        /// </summary>
        /// <param name="trigger"></param>
        public void SetUpdateTriggerOnNetworkStateChange(UpdateTriggerOnNetworkStateChange trigger)
        {
            SetSettingOfUpdateTrigger(SettingKey.UpdateTriggerOnNetworkStateChange, trigger);
        }

        /// <summary>
        ///  get <c>MiscellaneousSetting</c>.
        /// </summary>
        /// <returns></returns>
        public MiscellaneousSetting GetMiscellaneousSetting()
        {
            var setting = new MiscellaneousSetting();

            var state = GetSetting<ApplicationDataCompositeValue>(SettingKey.Miscelloaneous, null);
            if (state != null)
            {
                setting.LoadFrom(state);
            }

            return setting;
        }

        /// <summary>
        ///  set <c>MiscellaneousSetting</c>.
        /// </summary>
        /// <param name="setting"></param>
        public void SetMiscellaneousSetting(MiscellaneousSetting setting)
        {
            var state = new ApplicationDataCompositeValue();
            setting.SaveTo(state);
            SetSetting(SettingKey.Miscelloaneous, state);
        }

        #endregion

        public IDictionary<string, object> GetTileRegistration()
        {
            var state = GetSetting<ApplicationDataCompositeValue>(SettingKey.SecondaryTile, null);
            if (state == null)
            {
                state = new ApplicationDataCompositeValue();
            }
            return state;
        }

        public void SetTileRegistration(IDictionary<string, object> state)
        {
            var typedState = state as ApplicationDataCompositeValue;
            if (typedState == null)
            {
                throw new ArgumentException("state");
            }

            SetSetting(SettingKey.SecondaryTile, typedState);
        }
    }

    /// <summary>
    ///  設定のキー.
    /// </summary>
    internal enum SettingKey
    {
        /// <summary>ロック画面に設定するアイテムの登録数上限のキー</summary>
        MaxNumberOfItems, 

        /// <summary>ロック画面に設定するアイテムの登録数のキー</summary>
        NumberOfItems, 

        /// <summary>今ロック画面に設定されている(はずの)アイテムのインデックスのキー</summary>
        CurrentIndexOfItem,

        /// <summary>ロック画面に設定するアイテム一覧のキー</summary>
        Items,

        /// <summary>ロック画面を切り替えた日時</summary>
        TimeOfSetLockscreen,

        /// <summary>アプリ起動時にロック画面切り替えを行う設定のインデックスのキー</summary>
        UpdateTriggerOnAppLaunch, 

        /// <summary>一定時間ごと(AC電源接続時)にロック画面切り替えを行う設定のインデックスのキー</summary>
        UpdateTriggerOnMainenance, 

        /// <summary>ネットワークの状態が変わった時にロック画面切り替えを行う設定のインデックスのキー</summary>
        UpdateTriggerOnNetworkStateChange, 

        /// <summary>切り替えに関するその他の設定のインデックスのキー</summary>
        Miscelloaneous, 

        /// <summary>セカンダリタイルに関する設定のインデックスのキー</summary>
        SecondaryTile,
    }
}
