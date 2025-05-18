using System.ComponentModel;

public class SortableBindingList<T> : BindingList<T>
{
    private bool _isSorted;
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;
    private PropertyDescriptor _sortProperty;

    public SortableBindingList() : base()
    {
    }

    public SortableBindingList(IList<T> list) : base(list)
    {
    }

    protected override bool SupportsSortingCore => true;

    protected override bool IsSortedCore => _isSorted;

    protected override ListSortDirection SortDirectionCore => _sortDirection;

    protected override PropertyDescriptor SortPropertyCore => _sortProperty;

    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
    {
        _sortProperty = prop;
        _sortDirection = direction;

        if(prop == null)
        {
            _isSorted = false;
        }
        else
        {
            List<T> itemsList = Items as List<T>;
            if(itemsList == null)
            {
                itemsList = new List<T>(Items);
            }

            itemsList.Sort(Compare);

            _isSorted = true;
        }

        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    private int Compare(T x, T y)
    {
        if(_sortProperty == null)
            return 0;

        object xValue = _sortProperty.GetValue(x);
        object yValue = _sortProperty.GetValue(y);

        int comparisonResult;

        if(xValue == null && yValue == null)
        {
            comparisonResult = 0;
        }
        else if(xValue == null)
        {
            comparisonResult = -1;
        }
        else if(yValue == null)
        {
            comparisonResult = 1;
        }
        else if(xValue is IComparable comparableX)
        {
            comparisonResult = comparableX.CompareTo(yValue);
        }
        else if(xValue.Equals(yValue))
        {
            comparisonResult = 0;
        }
        else
        {
            comparisonResult = xValue.ToString().CompareTo(yValue.ToString());
        }

        return _sortDirection == ListSortDirection.Ascending ? comparisonResult : -comparisonResult;
    }


    protected override void RemoveSortCore()
    {
        _isSorted = false;
        _sortProperty = null;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

}