using System;
using System.Collections.Generic;

namespace DraftTwitchViewers
{
    public struct PaginationData
    {
        public string Cursor;
    }

    public class PaginatedArray<T>
    {
        public List<T> Data;

        public PaginationData Pagination;

        public int Total;

        public PaginatedArray()
        {
            Data = new List<T>();
        }

        public void AddPage(PaginatedArray<T> page)
        {
            Data.AddRange(page.Data);
            Pagination.Cursor = page.Pagination.Cursor;
            Total = page.Total;
        }
    }
}
