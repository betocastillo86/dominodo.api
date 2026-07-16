namespace Dominodo.Shared.Kernel.Pagination;

public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public int Skip => (Math.Max(Page, 1) - 1) * Take;
    public int Take => Math.Clamp(PageSize, 1, 100);
}
