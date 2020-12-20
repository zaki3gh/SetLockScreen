using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Background;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  ネットワークの状態が変わった時にロック画面切り替えを行う設定.
    /// </summary>
    public sealed class UpdateTriggerOnNetworkStateChange : UpdateTriggerBase
    {
        #region Main

        /// <summary><c>IsOn</c>プロパティの初期値</summary>
        const bool IsOnDefault = true;

        /// <summary>
        ///  Constructor.
        /// </summary>
        public UpdateTriggerOnNetworkStateChange()
        {
            this.IsOn = IsOnDefault;
        }

        #endregion

        #region Background Task

        /// <summary>
        ///  バックグラウンドタスクとして動作する場合に設定に対応するBackgroundTriggerを作成する.
        /// </summary>
        /// <returns><c>SystemTriggerType.NetworkStateChange</c>を返す</returns>
        public override IBackgroundTrigger MakeBackgroundTrigger()
        {
            return new SystemTrigger(SystemTriggerType.NetworkStateChange, false);
        }

        #endregion
    }
}
