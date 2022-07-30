using Microsoft.EntityFrameworkCore;

namespace Otakurin.Core.Common;

public class PagedListResult<T>
{
    public int Page { get; private set; }
    
    public int TotalPages { get; private set; }
    
    public int PageSize { get; private set; }
    
    public int CurrentPageSize { get; set; }
    
    public int CurrentStartIndex { get; set; }
    
    public int CurrentEndIndex { get; set; }
    
    public int TotalCount { get; private set; }

    public bool HasPrevious => Page > 1;
    
    public bool HasNext => Page < TotalPages;
    
    public List<T> Items { get; private set; }

    public PagedListResult(List<T> items, int count, int page, int pageSize)
    {
        TotalCount = count;
        PageSize = pageSize;
        Page = page;
        CurrentPageSize = items.Count;
        CurrentStartIndex = count == 0 ? 0 : ((page - 1) * pageSize) + 1;
        CurrentEndIndex = count == 0 ? 0 : CurrentStartIndex + CurrentPageSize - 1;
        TotalPages = (int) Math.Ceiling(count / (double) pageSize);
        Items = items;
    }

    public static PagedListResult<T> Create(IQueryable<T> source, int page, int pageSize)
    {
        var count = source.Count();
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedListResult<T>(items, count, page, pageSize);
    }

    public static async Task<PagedListResult<T>> CreateAsync(IQueryable<T> source, int page, int pageSize, CancellationToken cancellationToken)
    {
        var count = source.Count();
        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedListResult<T>(items, count, page, pageSize);
    }
}