using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace MyApps.SetLockscreen
{
    /// <summary>
    ///  ロック画面に設定するアイテムの登録リスト.
    /// </summary>
    public sealed class LockscreenItemList : ObservableCollection<LockscreenImageItem>
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public LockscreenItemList()
        {
            this.MaxNumberOfItems = AppSettings.Instance.MaxNumberOfItems;
        }

        /// <summary>
        ///  登録可能なアイテム数の上限.
        /// </summary>
        public int MaxNumberOfItems{ get; private set; }

        /// <summary>
        ///  登録リストが空の場合に<c>true</c>になる.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return this.Items.Count == 0;
            }
        }

        /// <summary>
        ///  登録リストが登録可能数上限に達している場合に<c>true</c>になる.
        /// </summary>
        public bool IsFull
        {
            get
            {
                return this.Items.Count >= this.MaxNumberOfItems;
            }
        }

        /// <summary>
        ///  登録可能数上限より多く登録されているアイテムの個数.
        /// </summary>
        public int NumberOverMax
        {
            get
            {
                return this.Items.Count - this.MaxNumberOfItems;
            }
        }

        /// <summary>
        ///  登録されているアイテムを読み込む.
        /// </summary>
        /// <returns></returns>
        public async Task LoadAllItemsAsync()
        {
            if (this.AlreadyLoaded)
            {
                return;
            }

            this.LoadingNow = true;
            await LoadAllItemsAsyncCore();
            this.LoadingNow = false;
            this.AlreadyLoaded = true;
        }

        /// <summary>
        ///  登録されているアイテムを読み込む(実装).
        /// </summary>
        /// <returns></returns>
        private async Task LoadAllItemsAsyncCore()
        {
            var itemsState = AppSettings.Instance.GetItemsState();
            if (itemsState == null)
            {
                return;
            }

            foreach (var setting in LockscreenImageItemSettingHelper.DeserializeItems(itemsState))
            {
                if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(setting.Token))
                {
                    continue;
                }

                bool succeeded = false;
                try
                {
                    var file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(setting.Token);
                    var item = new LockscreenImageItem(file) { Setting = setting };
                    Add(item);
                    succeeded = true;
                }
                catch (System.IO.FileNotFoundException)
                {
                    succeeded = false;
                }
                if (!succeeded)
                {
                    StorageApplicationPermissions.FutureAccessList.Remove(setting.Token);

                    if (!String.IsNullOrEmpty(setting.TileId))
                    {
                        await TileHelper.RemoveTileRegistrationAsync(setting.TileId);
                    }

                    continue;
                }
            }
        }

        /// <summary>
        ///  アイテムの状態をアプリの設定に保存する.
        /// </summary>
        public void SaveItemsToSetting()
        {
            AppSettings.Instance.SetItems(this);
        }

        /// <summary>
        ///  アイテムを登録追加する.
        /// </summary>
        /// <param name="item">アイテム</param>
        /// <returns></returns>
        public async Task AddItemAsync(LockscreenImageItem item)
        {
            Add(item);

            // 上限到達の場合、先頭のアイテムを削除し登録数のルールに合わせる.
            while (this.NumberOverMax > 0)
            {
                await RemoveAtAsync(0);
            }
        }

        /// <summary>
        ///  アイテムを登録解除する.
        /// </summary>
        /// <param name="index">アイテムのインデックス</param>
        /// <returns></returns>
        public async Task RemoveAtAsync(int index)
        {
            var item = this[index];
            await LockscreenImageItemSettingHelper.Remove(item);
            this.RemoveAt(index);
        }

        /// <summary>
        ///  アイテムを登録解除する.
        /// </summary>
        /// <param name="item">アイテム</param>
        /// <returns></returns>
        public async Task RemoveAsync(LockscreenImageItem item)
        {
            await LockscreenImageItemSettingHelper.Remove(item);
            this.Remove(item);
        }

        protected override void InsertItem(int index, LockscreenImageItem item)
        {
            // 重複させない
            if (this.Items.FirstOrDefault(x => x.Setting.Token.Equals(item.Setting.Token)) != null)
            {
                return;
            }

            if (!this.LoadingNow)
            {
                var currentIndex = AppSettings.Instance.CurrentIndex;
                if ((currentIndex == -1) || (currentIndex >= index))
                {
                    AppSettings.Instance.CurrentIndex += 1;
                }
            }

            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var currentIndex = AppSettings.Instance.CurrentIndex;
            if ((currentIndex >= index) && (currentIndex > 0))
            {
                AppSettings.Instance.CurrentIndex -= 1;
            }

            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            // 来ない...はず
            throw new NotImplementedException();
            //base.ClearItems();
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            // 来ない...はず
            throw new NotImplementedException();
            //base.MoveItem(oldIndex, newIndex);
        }

        protected override void SetItem(int index, LockscreenImageItem item)
        {
            // 来ない...はず
            throw new NotImplementedException();
            //base.SetItem(index, item);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!this.LoadingNow)
            {
                AppSettings.Instance.SetItems(this);
            }

            base.OnCollectionChanged(e);
        }

        /// <summary>
        ///  <c>true</c>なら登録されているアイテムを設定から読み込み中.
        /// </summary>
        public bool LoadingNow { get; private set; }

        /// <summary>
        ///  <c>true</c>なら登録されているアイテムを設定から読み込み済み.
        /// </summary>
        private bool AlreadyLoaded { get; set; }
    }
}
