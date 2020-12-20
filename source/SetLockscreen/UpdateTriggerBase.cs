using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Background;
using Windows.Data.Json;
using Windows.Storage;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  ロック画面切り替えを行うかどうかの設定の基本クラス.
    /// </summary>
    public abstract class UpdateTriggerBase : Common.BindableBase
    {
        #region Main

        /// <summary>
        ///  <c>true</c>ならロック画面切り替えを行う.
        /// </summary>
        public bool IsOn
        {
            get { return this.m_isOn; }
            set { SetProperty(ref this.m_isOn, value); }
        }

        /// <summary><c>IsOn</c>プロパティ.</summary>
        private bool m_isOn = IsOnDefault;

        /// <summary><c>IsOn</c>プロパティの初期値</summary>
        const bool IsOnDefault = true;

        #endregion

        #region Serialization

        /// <summary><c>IsOn</c>プロパティ用のJSONのキー</summary>
        private const string IsOnKey = "IsOn";

        /// <summary>
        ///  設定値をJSONに変換する.
        /// </summary>
        /// <returns>JSON</returns>
        public virtual JsonObject ToJson()
        {
            var obj = new JsonObject();
            obj.Add(IsOnKey, JsonValue.CreateBooleanValue(this.IsOn));
            return obj;
        }

        /// <summary>
        ///  設定値をJSONから変換する.
        ///  変換できない場合には何もしない.
        /// </summary>
        /// <param name="obj">JSON</param>
        public virtual void FromJson(JsonObject obj)
        {
            if (obj == null)
            {
                return;
            }

            var val = obj.GetNamedValue(IsOnKey);
            if ((val != null) && (val.ValueType == JsonValueType.Boolean))
            {
                this.IsOn = val.GetBoolean();
            }
        }

        /// <summary>
        ///  設定値をJSON文字列から変換する.
        ///  変換できない場合には何もしない.
        /// </summary>
        /// <param name="input">JSON文字列</param>
        public void FromJsonString(String input)
        {
            JsonObject obj;
            if (JsonObject.TryParse(input, out obj))
            {
                this.FromJson(obj);
            }
        }

        /// <summary>
        ///  設定値を保存する.
        /// </summary>
        /// <param name="state">保存先</param>
        public virtual void SaveTo(IDictionary<string, object> state)
        {
            state[IsOnKey] = this.IsOn;
        }

        /// <summary>
        ///  保存された設定値を読み込む.
        /// </summary>
        /// <param name="state">読み込み元</param>
        public virtual void LoadFrom(IDictionary<string, object> state)
        {
            object value;
            if (state.TryGetValue(IsOnKey, out value))
            {
                this.IsOn = (bool)value;
            }
        }

        #endregion

        #region Background Task

        /// <summary>
        ///  バックグラウンドタスクとしての名前.
        /// </summary>
        public virtual string TaskName
        {
            get { return this.GetType().Name; }
        }

        /// <summary>
        ///  バックグラウンドタスクとして動作する場合に設定に対応するBackgroundTriggerを作成する.
        /// </summary>
        /// <returns>既定の実装はバックグラウンドタスクではないため<c>null</c>を返す</returns>
        public virtual IBackgroundTrigger MakeBackgroundTrigger()
        {
            return null;
        }

        #endregion
    }
}
