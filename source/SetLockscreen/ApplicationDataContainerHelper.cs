using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

namespace MyApps.SetLockscreen
{
    static class ApplicationDataContainerHelper
    {
        public static T GetValue<T>(this ApplicationDataContainer container, string key, T defaultValue)
        {
            object value;
            if (container.Values.TryGetValue(key, out value))
            {
                return (T)value;
            }

            return defaultValue;
        }
    }
}
