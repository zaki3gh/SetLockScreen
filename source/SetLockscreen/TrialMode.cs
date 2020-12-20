using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Store;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  試用版の動作.
    /// </summary>
    internal class TrialMode
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public TrialMode()
        {
            this.licenseInfo = CurrentApp.LicenseInformation;
        }

        /// <summary>
        ///  登録可能なアイテム数.
        /// </summary>
        public int MaxNumberOfItems
        {
            get
            {
                if ((this.licenseInfo != null) && this.licenseInfo.IsActive)
                {
                    if (this.licenseInfo.IsTrial)
                    {
                        return MaxNumberOfItemsTrialMode;
                    }
                    else
                    {
                        return MaxNumberOfItemsFullLicense;
                    }
                }
                else
                {
                    return MaxNumberOfItemsTrialMode;
                }
            }
        }

        /// <summary>アプリのライセンス情報.</summary>
        private LicenseInformation licenseInfo;

        /// <summary>試用版での登録可能なアイテム数.</summary>
        private const int MaxNumberOfItemsTrialMode = 20;

        /// <summary>購入版での登録可能なアイテム数.</summary>
        private const int MaxNumberOfItemsFullLicense = 100;
    }
}
