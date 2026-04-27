using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Tewi.Game.Factory.Presentation
{
    public struct NativePagedTable<T> where T : unmanaged
    {
        public int pageShift;
        public int pageMask;
        public int maxPages;

        // 使用 NativeList 存储 Page 的引用
        private NativeArray<NativeArray<T>> _pages;
        private Allocator _allocator;

        public NativePagedTable(Allocator allocator, int pageShift = 12, int pageMask = 4095, int maxPages = 4096)
        {
            _allocator = allocator;
            this.pageShift = pageShift;
            this.pageMask = pageMask;
            this.maxPages = maxPages;
            _pages = new NativeArray<NativeArray<T>>(maxPages, allocator);
        }

        public unsafe void Set(int id, T data)
        {
            int pageIdx = id >> pageShift;
            int localIdx = id & pageMask;

            if (pageIdx >= maxPages)
                throw new InvalidOperationException("Exceeded maximum page limit (16,777,216).");

            if (!_pages[pageIdx].IsCreated)
            {
                _pages[pageIdx] = new NativeArray<T>(pageMask + 1, _allocator);
            }

            T* ptr = (T*)_pages[pageIdx].GetUnsafePtr();
            ptr[localIdx] = data;
        }

        public unsafe T Get(int id)
        {
            int pageIdx = id >> pageShift;
            if (!_pages[pageIdx].IsCreated) return default;

            T* ptr = (T*)_pages[pageIdx].GetUnsafeReadOnlyPtr();
            return ptr[id & pageMask];
        }

        public unsafe void RestorePageFromBytes(int pageIdx, byte[] data)
        {
            if (!_pages[pageIdx].IsCreated)
            {
                _pages[pageIdx] = new NativeArray<T>(pageMask + 1, _allocator);
            }

            fixed (void* src = data)
            {
                UnsafeUtility.MemCpy(_pages[pageIdx].GetUnsafePtr(), src, data.Length);
            }
        }

        public bool IsPageCreated(int index) => _pages[index].IsCreated;
        public NativeArray<T> GetPage(int index) => _pages[index];

        public void Dispose()
        {
            for (int i = 0; i < maxPages; i++)
            {
                if (_pages[i].IsCreated) _pages[i].Dispose();
            }
            _pages.Dispose();
        }
    }
}
