namespace BackendApi.Application.DTOs;

/// <summary>
/// Paginated result wrapper for API responses
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult()
    {
    }

    public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

/// <summary>
/// Pagination parameters for queries
/// </summary>
public class PaginationParams
{
    private int _pageNumber = 1;
    private int _pageSize = 10;
    private const int MaxPageSize = 100;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value > 0 ? value : 1;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 0 && value <= MaxPageSize ? value : 10;
    }

    public int Skip => (PageNumber - 1) * PageSize;
    public int Take => PageSize;
}

