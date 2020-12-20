using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  アプリ起動時にロック画面切り替えを行う設定.
    /// </summary>
    public sealed class UpdateTriggerOnAppLaunch : UpdateTriggerBase
    {
        #region Main

        /// <summary>
        ///  <c>true</c>なら切り替え前にユーザの確認を求める.
        /// </summary>
        public bool ShouldAskUser
        {
            get { return this.m_shouldAskUser; }
            set { SetProperty(ref this.m_shouldAskUser, value); }
        }

        /// <summary><c>ShouldAskUser</c>プロパティ</summary>
        private bool m_shouldAskUser = ShouldAskUserDefault;

        /// <summary><c>ShouldAskUser</c>プロパティの初期値</summary>
        const bool ShouldAskUserDefault = false;

        #endregion

        #region Serialization

        /// <summary><c>ShouldAskUser</c>プロパティ用のJSONのキー</summary>
        private const string ShouldAskUserKey = "ShouldAskUser";

        /// <summary>
        ///  設定値を保存する.
        /// </summary>
        /// <param name="state">保存先</param>
        public override void SaveTo(IDictionary<string, object> state)
        {
            base.SaveTo(state);
            state[ShouldAskUserKey] = this.ShouldAskUser;
        }

        /// <summary>
        ///  保存された設定値を読み込む.
        /// </summary>
        /// <param name="state">読み込み元</param>
        public override void LoadFrom(IDictionary<string, object> state)
        {
            base.LoadFrom(state);

            object value;
            if (state.TryGetValue(ShouldAskUserKey, out value))
            {
                this.ShouldAskUser = (bool)value;
            }
        }

        /// <summary>
        ///  設定値をJSONに変換する.
        /// </summary>
        /// <returns>JSON</returns>
        public override JsonObject ToJson()
        {
            var obj = base.ToJson();
            obj.Add(ShouldAskUserKey, JsonValue.CreateBooleanValue(this.ShouldAskUser));
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

            var val = obj.GetNamedValue(ShouldAskUserKey);
            if ((val != null) && (val.ValueType == JsonValueType.Boolean))
            {
                this.ShouldAskUser = val.GetBoolean();
            }
        }

        #endregion
    }
}
