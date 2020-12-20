using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;

namespace MyApps.SetLockscreen
{
    class TileRegistration
    {
        public string TileId { get; set; }
        public string NormalTileImageCacheFileName { get; set; }
        public string WideTileImageCacheFileName { get; set; }


        static string Key(TileRegistrationKey key)
        {
            return ((int)key).ToString();
        }

        public static TileRegistration FromJson(string input)
        {
            JsonObject jsonObj;
            if (!JsonObject.TryParse(input, out jsonObj))
            {
                return null;
            }

            return new TileRegistration()
            {
                TileId = jsonObj.GetNamedString(Key(TileRegistrationKey.TileId)),
                NormalTileImageCacheFileName = jsonObj.GetNamedString(Key(TileRegistrationKey.NormalTileImageCacheFileName)), 
                WideTileImageCacheFileName = jsonObj.GetNamedString(Key(TileRegistrationKey.WideTileImageCacheFileName)), 
            };
        }

        public JsonObject ToJson()
        {
            if (String.IsNullOrEmpty(this.TileId) ||
                String.IsNullOrEmpty(this.NormalTileImageCacheFileName) ||
                String.IsNullOrEmpty(this.WideTileImageCacheFileName))
            {
                throw new InvalidOperationException();
            }

            var jsonObj = new JsonObject();
            jsonObj.Add(Key(TileRegistrationKey.TileId), JsonValue.CreateStringValue(this.TileId));
            jsonObj.Add(Key(TileRegistrationKey.NormalTileImageCacheFileName), JsonValue.CreateStringValue(this.NormalTileImageCacheFileName));
            jsonObj.Add(Key(TileRegistrationKey.WideTileImageCacheFileName), JsonValue.CreateStringValue(this.WideTileImageCacheFileName));
            return jsonObj;
        }
    }

    enum TileRegistrationKey
    {
        TileId, 
        NormalTileImageCacheFileName, 
        WideTileImageCacheFileName,
    }

    static class TileRegistrationHelper
    {
        public static void Add(this IDictionary<string, object> state, TileRegistration registration)
        {
            state.Add(registration.TileId, registration.ToJson().Stringify());
        }

        public static TileRegistration At(this IDictionary<string, object> state, string tileId)
        {
            if (!state.ContainsKey(tileId))
            {
                return null;
            }

            return TileRegistration.FromJson(state[tileId] as string);
        }
    }
}
