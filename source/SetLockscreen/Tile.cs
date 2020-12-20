using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.StartScreen;
using Windows.Graphics.Imaging;


namespace MyApps.SetLockscreen
{
    public class Tile
    {
        /// <summary>正方形タイル用のイメージファイル名フォーマット</summary>
        private const string TileImageCacheFileNameFormatForNormal = TileHelper.TileImageCacheFileNameFormatForNormal; //@"{0}_n.png";

        /// <summary>ワイドタイル用のイメージファイル名フォーマット</summary>
        private const string TileImageCacheFileNameFormatForWide = TileHelper.TileImageCacheFileNameFormatForWide; //@"{0}_w.png";

        /// <summary>タイル用の画像を保存するフォルダー</summary>
        private const string TileImageCacheSubFolderName = @"TileImageCache";

        /// <summary>タイル用の画像を保存するフォルダーのUri</summary>
        private const string TileImageCacheUriPrefix = TileHelper.TileImageCacheUriPrefix;//@"ms-appdata:///local/TileImageCache/";

        /// <summary>正方形タイル用のイメージファイル</summary>
        private StorageFile normalLogoImageFile;

        /// <summary>ワイドタイル用のイメージファイル</summary>
        private StorageFile wideLogoImageFile;

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="item">ロック画面に設定するアイテム</param>
        private Tile()
        {
        }

        /// <summary>
        ///  セカンダリタイル.
        /// </summary>
        public SecondaryTile SecondaryTile { get; private set; }

        /// <summary>
        ///  タイル用のイメージファイルが作成済みかどうか.
        /// </summary>
        public bool IsTileLogoImageCreated
        {
            get
            {
                return (this.normalLogoImageFile != null) && (this.wideLogoImageFile != null);
            }
        }

        /// <summary>
        ///  ロック画面に設定するアイテムのセカンダリタイルを取得する.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static async Task<Tile> GetSecondaryTileForItem(LockscreenImageItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var secondaryTile = await TileHelper.GetSecondaryTileFromTileIdAsync(item.Setting.TileId);
            if (secondaryTile == null)
            {
                return null;
            }

            return new Tile()
            {
                SecondaryTile = secondaryTile,
            };
        }

        /// <summary>
        ///  セカンダリタイルを作成する.
        /// </summary>
        /// <param name="item"></param>
        public static async Task<Tile> CreateSecodaryTileForItem(LockscreenImageItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var tileId = TileHelper.GetNewTileId();
            if (tileId == null)
            {
                throw new InvalidOperationException("Too many tiles");
            }

            var tile = new Tile();
            await tile.CreateLogoImageFileForTileAsync(item, tileId);
            if (!tile.IsTileLogoImageCreated)
            {
                return null;
            }

            Uri normalLogoUri = new Uri(TileImageCacheUriPrefix + tile.normalLogoImageFile.Name);
            Uri wideLogoUri = new Uri(TileImageCacheUriPrefix + tile.wideLogoImageFile.Name);
            tile.SecondaryTile = new SecondaryTile(
                tileId,
                item.Name,
                Windows.ApplicationModel.Package.Current.Id.Name,
                TileHelper.TileArgumentsSetLockscreen,
                TileOptions.ShowNameOnLogo | TileOptions.ShowNameOnWideLogo,
                normalLogoUri,
                wideLogoUri);

            return tile;
        }

        /// <summary>
        ///  セカンダリタイル用のロゴ画像のファイルを作成する.
        /// </summary>
        /// <param name="item">ロック画面に設定するアイテム</param>
        /// <returns></returns>
        private async Task CreateLogoImageFileForTileAsync(LockscreenImageItem item, string tileId)
        {
            System.Diagnostics.Debug.Assert(item != null);
            if ((this.normalLogoImageFile != null) || (this.wideLogoImageFile != null))
            {
                throw new InvalidOperationException("Already created");
            }

            using (var inStream = await item.Item.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(inStream);
                var pixelDataProvider = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Premultiplied,
                    new BitmapTransform(),
                    ExifOrientationMode.RespectExifOrientation, 
                    ColorManagementMode.ColorManageToSRgb);
                var pixelData = pixelDataProvider.DetachPixelData();

                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(TileImageCacheSubFolderName, CreationCollisionOption.OpenIfExists);
                if (folder == null)
                {
                    return;
                }

                var normalFile = await CreateTileImageCacheFileAsync(
                    folder, 
                    String.Format(TileImageCacheFileNameFormatForNormal, tileId), 
                    150, 
                    150, 
                    decoder, 
                    pixelData);

                var wideFile = await CreateTileImageCacheFileAsync(
                    folder,
                    String.Format(TileImageCacheFileNameFormatForWide, tileId),
                    310,
                    150,
                    decoder,
                    pixelData);

                this.normalLogoImageFile = normalFile;
                this.wideLogoImageFile = wideFile;
            }
        }

        /// <summary>
        ///  タイル用のロゴ画像を指定されたサイズで作成する.
        /// </summary>
        /// <param name="folder">保存先フォルダー</param>
        /// <param name="fileName">保存するファイル名</param>
        /// <param name="width">保存するロゴ画像の幅</param>
        /// <param name="height">保存するロゴ画像の高さ</param>
        /// <param name="decoder"><c>BitmapDecoder</c></param>
        /// <param name="pixelData">画像のピクセルデータ</param>
        /// <returns></returns>
        private async Task<StorageFile> CreateTileImageCacheFileAsync(StorageFolder folder, string fileName, uint width, uint height, BitmapDecoder decoder, byte[] pixelData)
        {
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }
            if (decoder == null)
            {
                throw new ArgumentNullException("decoder");
            }
            if (width == 0)
            {
                throw new ArgumentOutOfRangeException("width");
            }
            if (height == 0)
            {
                throw new ArgumentOutOfRangeException("height");
            }
            if (pixelData == null)
            {
                throw new ArgumentNullException("pixelData");
            }

            var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
            if (file == null)
            {
                return null;
            }

            using (var outStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                if (outStream == null)
                {
                    return null;
                }

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.BitmapTransform.ScaledWidth = width;
                encoder.BitmapTransform.ScaledHeight = height;
                AdjustScaledSize(encoder.BitmapTransform, decoder.OrientedPixelWidth, decoder.OrientedPixelHeight);
                encoder.SetPixelData(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Ignore,
                    decoder.OrientedPixelWidth,
                    decoder.OrientedPixelHeight,
                    decoder.DpiX,
                    decoder.DpiY,
                    pixelData);
                await encoder.FlushAsync();
            }

            return file;
        }

        /// <summary>
        ///  ロック画面画像のサイズに応じて拡縮後のサイズを調整する.
        /// </summary>
        /// <param name="transform">調整対象</param>
        /// <param name="originalWidth">元のロック画面画像の幅</param>
        /// <param name="originalHeight">元のロック画面画像の高さ</param>
        private void AdjustScaledSize(BitmapTransform transform, uint originalWidth, uint originalHeight)
        {
            System.Diagnostics.Debug.Assert(transform != null);
            System.Diagnostics.Debug.Assert(originalWidth != 0);
            System.Diagnostics.Debug.Assert(originalHeight != 0);

            // rw = w / ow
            // rh = h / oh
            // if (oh * rw > h)  -> (ow * rh, h)
            //    (ow * rh > w)  -> (w, oh * rw)
            //
            //  oh * rw > h
            //  w / ow > h / oh
            //  w * oh > h * ow

            uint width = transform.ScaledWidth;
            uint height = transform.ScaledHeight;
            if (width * originalHeight > height * originalWidth)
            {
                width = Math.Min(width, originalWidth * height / originalHeight);

            }
            else
            {
                height = Math.Min(height, originalHeight * width / originalWidth);
            }

            transform.ScaledWidth = width;
            transform.ScaledHeight = height;
        }

        /// <summary>
        ///  セカンダリタイル用の画像ファイルを削除する.
        /// </summary>
        /// <returns></returns>
        public async Task DeleteTileImageCacheFilesAsync()
        {
            if (this.SecondaryTile == null)
            {
                throw new InvalidOperationException();
            }

            await TileHelper.DeleteSecondaryTileLogoFileAsync(this.SecondaryTile);
        }
    }

    /// <summary>
    ///  セカンダリタイルに関連する補助.
    /// </summary>
    public static class TileHelper
    {
        /// <summary>セカンダリタイルから起動したときのArguments</summary>
        public const string TileArgumentsSetLockscreen = "SetLockscreen";

        /// <summary>正方形タイル用のイメージファイル名フォーマット</summary>
        public const string TileImageCacheFileNameFormatForNormal = @"{0}_n.png";

        /// <summary>ワイドタイル用のイメージファイル名フォーマット</summary>
        public const string TileImageCacheFileNameFormatForWide = @"{0}_w.png";

        /// <summary>タイル用の画像を保存するフォルダーのUri</summary>
        public const string TileImageCacheUriPrefix = @"ms-appdata:///local/TileImageCache/";

        /// <summary>
        ///  指定されたタイルIdのセカンダリタイルがあればそれを取得する.
        /// </summary>
        /// <param name="tileId"></param>
        /// <returns></returns>
        public static async Task<SecondaryTile> GetSecondaryTileFromTileIdAsync(string tileId)
        {
            if (String.IsNullOrEmpty(tileId))
            {
                return null;
            }

            var tiles = await SecondaryTile.FindAllAsync();
            return tiles.FirstOrDefault(x => x.TileId.Equals(tileId));
        }

        /// <summary>
        ///  新規のタイルIdを取得する.
        /// </summary>
        /// <returns></returns>
        public static string GetNewTileId()
        {
            // ↓はうまくいかない場合が多いので時刻のticksにする
            //// 0からの連番で空いている番号を採番する.
            //var tiles = await SecondaryTile.FindAllAsync();

            //if (tiles.Count >= Int32.MaxValue)
            //{
            //    return null;
            //}

            //uint newId = 0;
            //foreach (var tile in tiles.OrderBy(x => UInt32.Parse(x.TileId)))
            //{
            //    var id = UInt32.Parse(tile.TileId);
            //    if (newId != id)
            //    {
            //        break;
            //    }
            //    ++newId;
            //}

            //return newId.ToString();

            var id = DateTimeOffset.UtcNow.Ticks;

            var regs = AppSettings.Instance.GetTileRegistration();
            while (regs.ContainsKey(id.ToString()))
            {
                ++id;
            }

            return id.ToString();
        }

        /// <summary>
        ///  セカンダリタイルが登録されているかどうかを確認する.
        /// </summary>
        /// <param name="tileId"></param>
        /// <returns></returns>
        public static bool IsTileRegistered(string tileId)
        {
            if (String.IsNullOrEmpty(tileId))
            {
                return false;
            }

            var regs = AppSettings.Instance.GetTileRegistration();
            return regs.ContainsKey(tileId);
        }

        /// <summary>
        ///  使われていないセカンダリタイルの<c>tileId</c>を登録情報から削除する.
        /// </summary>
        /// <returns></returns>
        public static async Task CleanupUnusedTileIdsAsync()
        {
            await Task.Run(async () =>
            {
                var tiles = await SecondaryTile.FindAllAsync();
                var tileIdSet = new HashSet<string>(tiles.Select(x => x.TileId));

                var regs = AppSettings.Instance.GetTileRegistration();
                foreach (var unusedId in regs.Where(x => !tileIdSet.Contains(x.Key)).Select(x => x.Key))
                {
                    await RemoveTileRegistrationAsync(unusedId);
                }
            });
        }

        /// <summary>
        ///  タイルの登録情報を保存する.
        /// </summary>
        /// <param name="tile"></param>
        public static void SaveTileRegistration(Tile tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException("tile");
            }

            var registration = new TileRegistration()
            {
                TileId = tile.SecondaryTile.TileId,
                NormalTileImageCacheFileName = tile.SecondaryTile.Logo.ToString(),
                WideTileImageCacheFileName = tile.SecondaryTile.WideLogo.ToString(),
            };

            var regs = AppSettings.Instance.GetTileRegistration();
            regs.Add(registration);
            AppSettings.Instance.SetTileRegistration(regs);
        }

        /// <summary>
        ///  タイルの登録情報を削除する.
        /// </summary>
        /// <param name="tileId"></param>
        /// <returns></returns>
        public static async Task RemoveTileRegistrationAsync(string tileId)
        {
            if (tileId == null)
            {
                throw new ArgumentNullException("tileId");
            }

            var regs = AppSettings.Instance.GetTileRegistration();
            if (!regs.ContainsKey(tileId))
            {
                return;
            }

            var registration = TileRegistration.FromJson(regs[tileId] as string);
            if (registration == null)
            {
                return;
            }

            await DeleteFileAsync(new Uri(registration.NormalTileImageCacheFileName));
            await DeleteFileAsync(new Uri(registration.WideTileImageCacheFileName));

            regs.Remove(tileId);
            AppSettings.Instance.SetTileRegistration(regs);
        }



        public static string GetNormalLogoFileNameForTildId(string tileId)
        {
            return String.Format(TileImageCacheFileNameFormatForNormal, tileId);
        }

        public static Uri GetNormalLogoUriForTildId(string tileId)
        {
            return new Uri(TileImageCacheUriPrefix + GetNormalLogoFileNameForTildId(tileId));
        }

        public static string GetWideLogoFileNameForTildId(string tileId)
        {
            return String.Format(TileImageCacheFileNameFormatForWide, tileId);
        }

        public static Uri GetWideLogoUriForTildId(string tileId)
        {
            return new Uri(TileImageCacheUriPrefix + GetWideLogoFileNameForTildId(tileId));
        }

        /// <summary>
        ///  セカンダリタイルのロゴ画像ファイルを削除する.
        /// </summary>
        /// <param name="tile">セカンダリタイル</param>
        /// <returns></returns>
        public static async Task DeleteSecondaryTileLogoFileAsync(SecondaryTile tile)
        {
            await DeleteFileAsync(tile.Logo);
            await DeleteFileAsync(tile.WideLogo);
        }

        /// <summary>
        ///  指定されたURIのファイルを削除する.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static async Task DeleteFileAsync(Uri uri)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            if (file != null)
            {
                await file.DeleteAsync();
            }
        }
    }
}
