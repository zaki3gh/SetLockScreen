using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Background;
using Windows.Data.Json;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  一定時間ごと(AC電源接続時)にロック画面切り替えを行う設定.
    /// </summary>
    public sealed class UpdateTriggerOnMaintenance : UpdateTriggerBase
    {
        #region Main

        /// <summary>
        ///  更新間隔(分).
        /// </summary>
        public UInt32 FreshnessTime
        {
            get { return this.m_freshnessTime; }
            set
            {
                if (value < s_freshnessTimeMinimum)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                SetProperty(ref this.m_freshnessTime, value);
            }
        }

        /// <summary><c>FreshnessTime</c>プロパティ</summary>
        private UInt32 m_freshnessTime = FreshnessTimeDefault;

        /// <summary><c>FreshnessTime</c>プロパティの初期値</summary>
        const UInt32 FreshnessTimeDefault = 480;

        /// <summary>
        ///  <c>FreshnessTime</c>プロパティの最小値.
        /// </summary>
        public UInt32 FreshnessTimeMinimum
        {
            get { return s_freshnessTimeMinimum; }
        }

        /// <summary><c>FreshnessTimeMinimum</c>プロパティ</summary>
        const UInt32 s_freshnessTimeMinimum = 15;

        /// <summary>
        ///  <c>FreshnessTime</c>プロパティの最大値.
        /// </summary>
        public UInt32 FreshnessTimeMaximum
        {
            get { return s_freshnessTimeMaximum; }
        }

        /// <summary><c>FreshnessTimeMaximum</c>プロパティ</summary>
        const UInt32 s_freshnessTimeMaximum = 1440;


        #endregion

        #region Serialization

        /// <summary><c>FreshnessTime</c>プロパティ用のJSONのキー</summary>
        const string FreshnessTimeKey = "FreshnessTime";

        /// <summary>
        ///  設定値を保存する.
        /// </summary>
        /// <param name="state">保存先</param>
        public override void SaveTo(IDictionary<string, object> state)
        {
            base.SaveTo(state);
            state[FreshnessTimeKey] = this.FreshnessTime;
        }

        /// <summary>
        ///  保存された設定値を読み込む.
        /// </summary>
        /// <param name="state">読み込み元</param>
        public override void LoadFrom(IDictionary<string, object> state)
        {
            base.LoadFrom(state);

            object value;
            if (state.TryGetValue(FreshnessTimeKey, out value))
            {
                this.FreshnessTime = (uint)value;
            }
        }

        /// <summary>
        ///  設定値をJSONに変換する.
        /// </summary>
        /// <returns>JSON</returns>
        public override JsonObject ToJson()
        {
            var obj = base.ToJson();
            obj.Add(FreshnessTimeKey, JsonValue.CreateNumberValue((double)this.FreshnessTime));
            return obj;
        }

        /// <summary>
        ///  設定値をJSONから変換する.
        ///  変換できない場合には何もしない.
        /// </summary>
        /// <param name="obj">JSON</param>
        public override void FromJson(JsonObject obj)
        {
            if (obj == null)
            {
                return;
            }

            base.FromJson(obj);

            var val = obj.GetNamedValue(FreshnessTimeKey);
            if ((val != null) && (val.ValueType == JsonValueType.Number))
            {
                this.FreshnessTime = (uint)val.GetNumber();
            }
        }

        #endregion

        #region Background Task

        /// <summary>
        ///  バックグラウンドタスクとして動作する場合に設定に対応するBackgroundTriggerを作成する.
        /// </summary>
        /// <returns><c>MaintenanceTrigger</c>を返す</returns>
        public override IBackgroundTrigger MakeBackgroundTrigger()
        {
            return new MaintenanceTrigger(this.FreshnessTime, false);
        }

        #endregion
    }
}
