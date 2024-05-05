namespace EAFC.Core.Models;

public class Pagination<T>(List<T> items, int count, int pageNumber, int pageSize)
{
    public int CurrentPage { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public int TotalCount { get; set; } = count;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public List<T> Items { get; set; } = items;

    public bool Any(Func<T, bool> func)
    {
        return Items.Any(func);
    }
}