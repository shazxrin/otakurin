namespace Otakurin.Core.Common;

public abstract class PagedListRequest
{
    protected virtual int MaxPageSize { get; } = 20;
    protected virtual int DefaultPageSize { get; set; } = 10;

    public virtual int Page { get; set; } = 1;

    public int PageSize
    {
        get => DefaultPageSize;
        set => DefaultPageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}