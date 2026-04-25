using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Tewi.Game.Factory.Presentation
{
    public struct NativePagedTable<T> where T : unmanaged
    {
        private const int PAGE_SHIFT = 10;
        private const int PAGE_MASK = 1023;
        private const int MAX_PAGES = 1024;

        // 使用 NativeList 存储 Page 的引用
        private NativeArray<NativeArray<T>> _pages;
        private Allocator _allocator;

        public NativePagedTable(Allocator allocator)
        {
            _allocator = allocator;
            _pages = new NativeArray<NativeArray<T>>(MAX_PAGES, allocator);
        }

        public unsafe void Set(int id, T data)
        {
            int pageIdx = id >> PAGE_SHIFT;
            int localIdx = id & PAGE_MASK;

            if (!_pages[pageIdx].IsCreated)
            {
                _pages[pageIdx] = new NativeArray<T>(PAGE_MASK + 1, _allocator);
            }

            // 获取该页的原始指针
            T* ptr = (T*)_pages[pageIdx].GetUnsafePtr();

            // 直接操作内存，没有任何拷贝
            ptr[localIdx] = data;
        }

        public unsafe T Get(int id)
        {
            int pageIdx = id >> PAGE_SHIFT;
            if (!_pages[pageIdx].IsCreated) return default;

            T* ptr = (T*)_pages[pageIdx].GetUnsafeReadOnlyPtr();
            return ptr[id & PAGE_MASK];
        }

        public void Dispose()
        {
            for (int i = 0; i < MAX_PAGES; i++)
            {
                if (_pages[i].IsCreated) _pages[i].Dispose();
            }
            _pages.Dispose();
        }
    }
}
