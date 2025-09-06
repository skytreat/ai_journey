public interface IPivotSelector<T>
{
    T SelectPivot(IList<T> list);
}

public interface IPartitioner<T>
{
    (IList<T> left, IList<T> right) Partition(IList<T> list, T pivot);
}

public class FirstElementPivotSelector<T> : IPivotSelector<T>
{
    public T SelectPivot(IList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty.");

        return list[0];
    }
}

public class MiddleElementPivotSelector<T> : IPivotSelector<T>
{
    public T SelectPivot(IList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty.");

        return list[list.Count / 2];
    }
}

public class RandomElementPivotSelector<T> : IPivotSelector<T>
{
    private readonly Random _random = new Random();

    public T SelectPivot(IList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty.");

        int index = _random.Next(list.Count);
        return list[index];
    }
}

public class HoarePartitioner<T> : IPartitioner<T> where T : IComparable<T>
{
    public (IList<T> left, IList<T> right) Partition(IList<T> list, T pivot)
    {
        var left = new List<T>();
        var right = new List<T>();

        foreach (var item in list)
        {
            if (item.CompareTo(pivot) <= 0)
                left.Add(item);
            else
                right.Add(item);
        }

        return (left, right);
    }
}

public class LomutoPartitioner<T> : IPartitioner<T> where T : IComparable<T>
{
    public (IList<T> left, IList<T> right) Partition(IList<T> list, T pivot)
    {
        var left = new List<T>();
        var right = new List<T>();

        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].CompareTo(pivot) <= 0)
                left.Add(list[i]);
            else
                right.Add(list[i]);
        }

        left.Add(pivot);
        left.AddRange(right);

        if (left.Count > 0)
            right = left.Skip(left.IndexOf(pivot) + 1).ToList();

        return (left.Take(left.IndexOf(pivot)).ToList(), right);
    }
}

public abstract class QuickSortBase<T> where T : IComparable<T>
{
    protected readonly IPivotSelector<T> _pivotSelector;
    protected readonly IPartitioner<T> _partitioner;

    public QuickSortBase(IPivotSelector<T> pivotSelector, IPartitioner<T> partitioner)
    {
        _pivotSelector = pivotSelector ?? throw new ArgumentNullException(nameof(pivotSelector));
        _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
    }

    public void Sort(IList<T> list)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        QuickSort(list, 0, list.Count - 1);
    }

    protected virtual void QuickSort(IList<T> list, int left, int right)
    {
        if (left >= right)
            return;

        T pivot = _pivotSelector.SelectPivot(list);
        var (leftPartition, rightPartition) = _partitioner.Partition(list, pivot);

        QuickSort(leftPartition, 0, leftPartition.Count - 1);
        QuickSort(rightPartition, 0, rightPartition.Count - 1);
    }
}

public class QuickSortFactory<T> where T : IComparable<T>
{
    private readonly IServiceProvider _serviceProvider;

    public QuickSortFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public QuickSort<T> CreateQuickSort(PivotSelectionStrategy strategy = PivotSelectionStrategy.FirstElement, 
        PartitioningMethod method = PartitioningMethod.Hoare)
    {
        IPivotSelector<T> pivotSelector;
        IPartitioner<T> partitioner;

        switch (strategy)
        {
            case PivotSelectionStrategy.FirstElement:
                pivotSelector = new FirstElementPivotSelector<T>();
                break;
            case PivotSelectionStrategy.MiddleElement:
                pivotSelector = new MiddleElementPivotSelector<T>();
                break;
            case PivotSelectionStrategy.RandomElement:
                pivotSelector = new RandomElementPivotSelector<T>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
        }

        switch (method)
        {
            case PartitioningMethod.Hoare:
                partitioner = new HoarePartitioner<T>();
                break;
            case PartitioningMethod.Lomuto:
                partitioner = new LomutoPartitioner<T>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, null);
        }

        var logger = _serviceProvider.GetService(typeof(ILogger<QuickSort<T>>)) as ILogger<QuickSort<T>>;
        return new QuickSort<T>(pivotSelector, partitioner, logger);
    }
}

public enum PivotSelectionStrategy
{
    FirstElement,
    MiddleElement,
    RandomElement
}

public enum PartitioningMethod
{
    Hoare,
    Lomuto
}

/// <summary>
/// Implements the QuickSort algorithm with support for various pivot selection and partitioning strategies.
/// </summary>
/// <typeparam name="T">The type of elements in the list to be sorted.</typeparam>
public class QuickSort<T> : QuickSortBase<T> where T : IComparable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuickSort{T}"/> class with the specified pivot selector and partitioner.
    /// </summary>
    /// <param name="pivotSelector">The strategy used to select the pivot element.</param>
    /// <param name="partitioner">The method used to partition the list around the pivot.</param>
    /// <param name="logger">The logger instance for logging events and errors.</param>
    public QuickSort(IPivotSelector<T> pivotSelector, IPartitioner<T> partitioner) 
        : base(pivotSelector, partitioner)
    {
    }
}

public class QuickSortTests
{
    [Fact]
    public void QuickSort_SortList_ListIsSorted()
    {
        // Arrange
        var list = new List<int> { 5, 2, 8, 3, 1, 6, 4 };
        var pivotSelector = new FirstElementPivotSelector<int>();
        var partitioner = new HoarePartitioner<int>();
        var quickSort = new QuickSort<int>(pivotSelector, partitioner);

        // Act
        quickSort.Sort(list);

        // Assert
        Assert.True(list.SequenceEqual(new List<int> { 1, 2, 3, 4, 5, 6, 8 }));
    }

    [Fact]
    public void QuickSort_SortEmptyList_NoOperationPerformed()
    {
        // Arrange
        var list = new List<int>();
        var pivotSelector = new FirstElementPivotSelector<int>();
        var partitioner = new HoarePartitioner<int>();
        var quickSort = new QuickSort<int>(pivotSelector, partitioner);

        // Act
        quickSort.Sort(list);

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public void QuickSort_SortSingleElementList_NoChange()
    {
        // Arrange
        var list = new List<int> { 7 };
        var pivotSelector = new FirstElementPivotSelector<int>();
        var partitioner = new HoarePartitioner<int>();
        var quickSort = new QuickSort<int>(pivotSelector, partitioner);

        // Act
        quickSort.Sort(list);

        // Assert
        Assert.Single(list);
        Assert.Equal(7, list[0]);
    }

    [Fact]
    public void QuickSort_SortListWithDuplicates_DuplicatesHandledCorrectly()
    {
        // Arrange
        var list = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6, 5 };
        var pivotSelector = new FirstElementPivotSelector<int>();
        var partitioner = new HoarePartitioner<int>();
        var quickSort = new QuickSort<int>(pivotSelector, partitioner);

        // Act
        quickSort.Sort(list);

        // Assert
        Assert.True(list.SequenceEqual(new List<int> { 1, 1, 2, 3, 4, 5, 5, 6, 9 }));
    }

    [Fact]
    public void QuickSort_NullList_ThrowsArgumentNullException()
    {
        // Arrange
        List<int> list = null;
        var pivotSelector = new FirstElementPivotSelector<int>();
        var partitioner = new HoarePartitioner<int>();
        var quickSort = new QuickSort<int>(pivotSelector, partitioner);

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => quickSort.Sort(list));
    }
}