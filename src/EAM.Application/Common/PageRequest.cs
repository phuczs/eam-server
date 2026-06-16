namespace EAM.Application.Common;

/// <summary>Paging input with sane clamping (1-based page, capped size).</summary>
public class PageRequest
{
    private const int MaxSize = 100;
    private int _size = 20;
    private int _page = 1;

    public int Page { get => _page; set => _page = value < 1 ? 1 : value; }
    public int Size { get => _size; set => _size = value is < 1 or > MaxSize ? 20 : value; }

    public int Skip => (Page - 1) * Size;
}
