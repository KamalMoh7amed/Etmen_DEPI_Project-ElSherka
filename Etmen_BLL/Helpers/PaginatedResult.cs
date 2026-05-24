namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Represents a paginated result set returned from service layer.
    /// </summary>
    public class PaginatedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public static PaginatedResult<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
        {
            var items = source.ToList();
            return new PaginatedResult<T>
            {
                Items = items,
                TotalCount = items.Count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public static PaginatedResult<T> Create(IEnumerable<T> source, int totalCount, int pageNumber, int pageSize)
        {
            return new PaginatedResult<T>
            {
                Items = source.ToList().AsReadOnly(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
