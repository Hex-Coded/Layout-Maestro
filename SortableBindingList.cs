using System.ComponentModel;

public class SortableBindingList<T> : BindingList<T>
{
    bool isSorted;
    ListSortDirection sortDirection = ListSortDirection.Ascending;
    PropertyDescriptor sortProperty;

    public SortableBindingList() : base()
    {
    }

    public SortableBindingList(IList<T> list) : base(list)
    {
    }

    protected override bool SupportsSortingCore => true;

    protected override bool IsSortedCore => isSorted;

    protected override ListSortDirection SortDirectionCore => sortDirection;

    protected override PropertyDescriptor SortPropertyCore => sortProperty;

    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
    {
        sortProperty = prop;
        sortDirection = direction;

        if(prop == null)
        {
            isSorted = false;
        }
        else
        {
            List<T> itemsList = Items as List<T>;
            if(itemsList == null)
            {
                itemsList = new List<T>(Items);
            }

            itemsList.Sort(Compare);

            isSorted = true;
        }

        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    int Compare(T x, T y)
    {
        if(sortProperty == null)
            return 0;

        object xValue = sortProperty.GetValue(x);
        object yValue = sortProperty.GetValue(y);

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

        return sortDirection == ListSortDirection.Ascending ? comparisonResult : -comparisonResult;
    }

    protected override void RemoveSortCore()
    {
        isSorted = false;
        sortProperty = null;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }
}