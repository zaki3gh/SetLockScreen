using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  切り替えの最小間隔の設定.
    /// </summary>
    public sealed class MiscellaneousSetting : Common.BindableBase
    {

        #region Main

        /// <summary>
        ///  切り替えの最小間隔(分).
        /// </summary>
        public UInt32 SuppressedDuration
        {
            get { return this.suppressedDuration; }
            set { SetProperty(ref this.suppressedDuration, value); }
        }

        /// <summary><c>SuppressedDuration</c>プロパティ</summary>
        private UInt32 suppressedDuration = SuppressedDurationDefault;

        /// <summary><c>SuppressedDuration</c>プロパティの初期値</summary>
        const UInt32 SuppressedDurationDefault = 60;

        /// <summary><c>SuppressedDuration</c>プロパティの最小値</summary>
        public UInt32 SuppressedDurationMinimum { get { return s_suppressedDurationMinimum;} }

        /// <summary><c>SuppressedDurationMinimum</c>プロパティ</summary>
        const UInt32 s_suppressedDurationMinimum = 0;

        /// <summary><c>SuppressedDuration</c>プロパティの最大値</summary>
        public UInt32 SuppressedDurationMaximum { get { return s_suppressedDurationMaximum;} }

        /// <summary><c>SuppressedDurationMaximum</c>プロパティ</summary>
        const UInt32 s_suppressedDurationMaximum = 240;

        /// <summary>
        ///  切り替えモード.
        /// </summary>
        public UpdateMode UpdateMode
        {
            get { return this.updateMode; }
            set { SetProperty(ref this.updateMode, value); }
        }

        /// <summary><c>UpdateMode</c>プロパティ</summary>
        private UpdateMode updateMode;

        /// <summary><c>UpdateMode</c>プロパティの初期値</summary>
        const UpdateMode UpdateModeDefault = UpdateMode.Normal;

        #endregion

        #region Serialization

        /// <summary><c>SuppprssedDuration</c>プロパティ用のキー</summary>
        private const string SuppressedDurationKey = "SuppressedDuration";

        /// <summary><c>UpdateMode</c>プロパティ用のキー</summary>
        private const string UpdateModeKey = "UpdateMode";

        /// <summary>
        ///  設定値を保存する.
        /// </summary>
        /// <param name="state">保存先</param>
        public void SaveTo(IDictionary<string, object> state)
        {
            state[SuppressedDurationKey] = this.SuppressedDuration;
            state[UpdateModeKey] = (int)this.UpdateMode;
        }

        /// <summary>
        ///  保存された設定値を読み込む.
        /// </summary>
        /// <param name="state">読み込み元</param>
        public void LoadFrom(IDictionary<string, object> state)
        {
            object value;
            if (state.TryGetValue(SuppressedDurationKey, out value))
            {
                this.SuppressedDuration = (UInt32)value;
            }

            object valUpdateMode;
            if (state.TryGetValue(UpdateModeKey, out valUpdateMode))
            {
                this.UpdateMode = (UpdateMode)(int)valUpdateMode;
            }
        }

        #endregion
    }

    /// <summary>
    ///  切り替え方法.
    /// </summary>
    public enum UpdateMode
    {
        /// <summary>並べられた順に切り替えていく</summary>
        Normal = 0, 

        /// <summary>ランダムに切り替えていく</summary>
        Shuffle,
    }
}
