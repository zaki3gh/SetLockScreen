using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  ロック画面に設定するアイテム.
    /// </summary>
    public class LockscreenImageItem : Common.BindableBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="file">ロック画面に設定するファイル</param>
        public LockscreenImageItem(StorageFile file)
        {
            this.Item = file;
        }

        /// <summary>
        ///  アイテムが表しているファイル.
        /// </summary>
        public Windows.Storage.StorageFile Item { get; private set; }

        /// <summary>
        ///  登録情報.
        /// </summary>
        public LockscreenImageItemSetting Setting { get; set; }

        /// <summary>
        ///  アイテムの表示名.
        /// </summary>
        public string Name
        {
            get { return this.Item.Name; }
        }

        /// <summary>
        ///  アイテムのサムネイル画像.
        /// </summary>
        public Windows.UI.Xaml.Media.Imaging.BitmapImage Image
        {
            get
            {
                if (this.m_thumbnail != null)
                {
                    Windows.UI.Xaml.Media.Imaging.BitmapImage image;
                    if (this.m_thumbnail.TryGetTarget(out image))
                    {
                        return image;
                    }
                }

                GetThumbnailAsync();
                return null;
            }
        }

        /// <summary>
        ///  <c>Image</c>プロパティ.
        /// </summary>
        WeakReference<Windows.UI.Xaml.Media.Imaging.BitmapImage> m_thumbnail;

        /// <summary>
        ///  アイテムのサムネイル画像を取得する.
        /// </summary>
        async void GetThumbnailAsync()
        {
            if (this.Item == null)
            {
                return;
            }

            using (var thumbnail = await this.Item.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem))
            {
                var bmpImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                await bmpImage.SetSourceAsync(thumbnail);
                if (this.m_thumbnail == null)
                {
                    this.m_thumbnail = new WeakReference<Windows.UI.Xaml.Media.Imaging.BitmapImage>(bmpImage);
                }
                else
                {
                    this.m_thumbnail.SetTarget(bmpImage);
                }
                OnPropertyChanged("Image");
            }
        }
    }

    /// <summary>
    ///  ロック画面に設定するアイテムの登録情報.
    /// </summary>
    public class LockscreenImageItemSetting
    {
        /// <summary>
        ///  <c>FutureAccessList</c>に指定する<c>token</c>.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        ///  一時ファイルかどうかを示すフラグ.
        /// </summary>
        public bool IsTemporary { get; set; }

        /// <summary>
        ///  セカンダリタイルとして登録されている場合のタイルId.
        /// </summary>
        public string TileId { get; set; }
    }

    /// <summary>
    ///  <c>LockscreenImageItemSetting</c>のプロパティのインデックス.
    /// </summary>
    public enum LockscreenImageItemSettingProperty
    {
        /// <summary><c>Token</c>プロパティ</summary>
        Token,

        /// <summary><c>IsTemporary</c>プロパティ</summary>
        IsTemporary,

        /// <summary><c>TileId</c>プロパティ</summary>
        TileId, 
    }

    /// <summary>
    ///  <c>LockscreenImageItem</c>, <c>LockscreenImageItemSetting</c>の補助関数群.
    /// </summary>
    public static class LockscreenImageItemSettingHelper
    {
        /// <summary>
        ///  指定したファイルを対象とする「ロック画面に設定するアイテム」を作成する.
        /// </summary>
        /// <param name="file">ファイル</param>
        /// <returns>ロック画面に設定するアイテム</returns>
        public static async Task<LockscreenImageItem> FromStorageItemAsync(StorageFile file, bool isTemporaryDefault, System.Threading.CancellationToken cancellationToken)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

//            await Task.Delay(10 * 1000, cancellationToken);

            try
            {
                // temporaryはLocalFolderにKeepする
                //  + remove時に削除できるようにフラグをset
                var fileAdded = file;
                bool isTemporary = isTemporaryDefault;
                if (file.Attributes.HasFlag(FileAttributes.Temporary))
                {
                    isTemporary = true;
                    var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(@"TempFileCache", CreationCollisionOption.OpenIfExists).AsTask(cancellationToken);
                    fileAdded = await file.CopyAsync(folder, file.Name, NameCollisionOption.GenerateUniqueName).AsTask(cancellationToken);
                }

                var token = StorageApplicationPermissions.FutureAccessList.Add(fileAdded);
                var setting = new LockscreenImageItemSetting() { Token = token, IsTemporary = isTemporary };
                return new LockscreenImageItem(fileAdded) { Setting = setting, };
            }
            catch (System.IO.FileNotFoundException /*ex*/)
            {
                return null;
            }
        }

        /// <summary>
        ///  「ロック画面に設定するアイテム」を登録解除する.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static async Task Remove(LockscreenImageItem item)
        {
            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(item.Setting.Token))
            {
                StorageApplicationPermissions.FutureAccessList.Remove(item.Setting.Token);
            }

            if (item.Setting.IsTemporary)
            {
                await item.Item.DeleteAsync();
            }

            if (!String.IsNullOrEmpty(item.Setting.TileId))
            {
                await TileHelper.RemoveTileRegistrationAsync(item.Setting.TileId);
                item.Setting.TileId = null;
            }
        }

        /// <summary>
        ///  JSON文字列から<c>LockscreenImageItemSetting</c>を復元する.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static LockscreenImageItemSetting FromJson(string input)
        {
            JsonObject jsonObj;
            if (!JsonObject.TryParse(input, out jsonObj))
            {
                return null;
            }

            return new LockscreenImageItemSetting()
            {
                Token = jsonObj.GetNamedString(LockscreenImageItemSettingProperty.Token.ToString()),
                IsTemporary = jsonObj.GetNamedBoolean(LockscreenImageItemSettingProperty.IsTemporary.ToString()), 
                TileId = jsonObj.ContainsKey(LockscreenImageItemSettingProperty.TileId.ToString()) ? jsonObj.GetNamedString(LockscreenImageItemSettingProperty.TileId.ToString()) : null, 
            };
        }

        /// <summary>
        ///  <c>LockscreenImageItemSetting</c>をJSONに変換する.
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static JsonObject ToJson(this LockscreenImageItemSetting setting)
        {
            var jsonObj = new JsonObject();
            jsonObj.Add(LockscreenImageItemSettingProperty.Token.ToString(), JsonValue.CreateStringValue(setting.Token));
            jsonObj.Add(LockscreenImageItemSettingProperty.IsTemporary.ToString(), JsonValue.CreateBooleanValue(setting.IsTemporary));
            if (!String.IsNullOrEmpty(setting.TileId))
            {
                jsonObj.Add(LockscreenImageItemSettingProperty.TileId.ToString(), JsonValue.CreateStringValue(setting.TileId));
            }
            return jsonObj;
        }

        /// <summary>
        ///  ロック画面に設定するアイテムをシリアライズする.
        /// </summary>
        /// <param name="items">ロック画面に設定するアイテムの列挙</param>
        /// <param name="state"><paramref name="items"/>のシリアライズ結果を格納する<c>Dictionary</c></param>
        public static void SerializeItems(this IEnumerable<LockscreenImageItem> items, IDictionary<string, object> state)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            uint index = 0;
            foreach (var item in items)
            {
                var json = item.Setting.ToJson().Stringify();
                state[index.ToString()] = json;
                ++index;
            }
        }

        /// <summary>
        ///  ロック画面に設定するアイテムのシリアライズ結果から指定されたインデックスの登録情報をデシリアライズする.
        /// </summary>
        /// <param name="index">デシリアライズするインデックス</param>
        /// <param name="state">ロック画面に設定するアイテムのシリアライズ結果</param>
        /// <returns>登録情報</returns>
        public static LockscreenImageItemSetting DeserializeItemAt(int index, IDictionary<string, object> state)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            object value;
            if (!state.TryGetValue(index.ToString(), out value))
            {
                return null;
            }

            return FromJson(value as string);
        }

        /// <summary>
        ///  ロック画面に設定するアイテムのシリアライズ結果をデシリアライズする.
        /// </summary>
        /// <param name="state">ロック画面に設定するアイテムのシリアライズ結果</param>
        /// <returns>登録情報の列挙</returns>
        public static IEnumerable<LockscreenImageItemSetting> DeserializeItems(IDictionary<string, object> state)
        {
            int index = 0;
            var item = DeserializeItemAt(index, state);
            while (item != null)
            {
                yield return item;
                ++index;
                item = DeserializeItemAt(index, state);
            }
        }
    }
}
