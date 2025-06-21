using System.Collections.Generic;

namespace Frontend.Models.Common
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (PageSize > 0) ? (TotalCount + PageSize - 1) / PageSize : 0;
    }
}
