using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading;


namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  共有で渡される登録候補のファイル.
    /// </summary>
    class SharedStorageFileItem : ISharedItem
    {
        /// <summary>
        ///  共有の<c>DataPackageView</c>からこのクラスのインスタンスを作成する.
        /// </summary>
        /// <param name="package">共有の<c>DataPackageView</c></param>
        /// <returns>このクラスのインスタンス</returns>
        public static async Task<List<SharedStorageFileItem>> CreateFromDataPackage(DataPackageView package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            if (!package.Contains(StandardDataFormats.StorageItems))
            {
                return null;
            }

            try
            {
                var items = await package.GetStorageItemsAsync();
                if (items == null)
                {
                    return null;
                }

                var sharedItems = new List<SharedStorageFileItem>(items.Count);
                foreach (var item in items.Where(x => x.IsOfType(StorageItemTypes.File)))
                {
                    // ここで↓のようにサムネイル画像を取得するといくつかのアプリからの共有で
                    // サムネイル画像がアプリアイコン相当になってしまう.
                    // ファイルの内容をすべてreadするようにしたところ見られるようになったので
                    // 負荷は高そうだがそうするように変更.
                    // なお、なぜかShare contents sourceサンプルからの共有では問題なし。
                    // File Pickerを一度経由しているからかもしれない.
                    //using (var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem))

                    var file = item as StorageFile;

                    // 画像ファイル以外を排除
                    var properties = await file.Properties.GetImagePropertiesAsync();
                    if ((properties.Width == 0) || (properties.Height == 0))
                    {
                        return null;
                    }

                    using (var thumbnail = await file.OpenAsync(FileAccessMode.Read))
                    {
                        var bmpImage = new BitmapImage();
                        await bmpImage.SetSourceAsync(thumbnail);
                        sharedItems.Add(new SharedStorageFileItem(file, bmpImage));
                    }
                }

                return sharedItems;
            }
            // GetStorageItemsAsync()が失敗する
            //  経験: FreshPaintで共有する
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80070490u)
                {
                    return null;
                }
                // FORTMATETC構造体が無効
                if ((uint)ex.HResult == 0x80040064u)
                {
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="thumbnailImage"></param>
        public SharedStorageFileItem(StorageFile file, BitmapImage thumbnailImage)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            if (thumbnailImage == null)
            {
                throw new ArgumentNullException("thumbnailImage");
            }

            this.File = file;
            this.Image = thumbnailImage;
        }

        /// <summary>
        ///  アイテムのサムネイル画像.
        /// </summary>
        public BitmapImage Image { get; private set; }

        /// <summary>
        ///  ファイル.
        /// </summary>
        public StorageFile File { get; private set; }

        /// <summary>
        ///  一時ファイルとして扱うかどうか.
        /// </summary>
        public bool IsTemporary { get { return false; } }

        /// <summary>
        ///  ロック画面登録用のファイルを準備する.
        /// </summary>
        /// <returns>準備済みなので<c>null</c>が返る</returns>
        public Task PrepareFileAsync()
        {
            return null;
        }

        /// <summary>
        ///  ロック画面に設定する.
        /// </summary>
        /// <returns></returns>
        public async Task SetLockscreenAsync()
        {
            if (this.File == null)
            {
                return;
            }

            bool succeedToSet = false;
            try
            {
                await Windows.System.UserProfile.LockScreen.SetImageFileAsync(this.File);
                succeedToSet = true;
            }
            catch (UnauthorizedAccessException)
            {
                // メイン画面で↓のようなことがあったのでこちらも同じようにしておく
                // 共有ターゲットではクリップボードからの貼り付けではないので不要かもしれないが

                // 画像ファイルを貼り付けしてその直後に
                // ロック画面に設定しようとすると↑はアクセス拒否で失敗する。
                // その場合でも↓で問題ないのでそれを試す。
                // もしかしたら常に↓でよいのかもしれない...
            }

            if (!succeedToSet)
            {
                using (var strm = await this.File.OpenReadAsync())
                {
                    await Windows.System.UserProfile.LockScreen.SetImageStreamAsync(strm);
                    succeedToSet = true;
                }
            }
        }
    }
}
