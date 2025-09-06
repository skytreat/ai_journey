class QuickSort
{
    /// <summary>
    /// Sorts a list of integers using the QuickSort algorithm.
    /// This method implements the in-place QuickSort algorithm, which 
    /// recursively partitions the input list around a pivot element.
    /// </summary>
    /// <param name="inputList">The list of integers to be sorted. This list is modified in place.</param>
    /// <exception cref="NullReferenceException">Thrown if the inputList is null.</exception>
    public static void QuickSort_V1(List<int> inputList)
    {
        int pivot = inputList[0];
        List<int> leftList = new List<int>();
        List<int> rightList = new List<int>();

        for (int i = 1; i < inputList.Count; i++)
        {
            if (inputList[i] < pivot)
            {
                leftList.Add(inputList[i]);
            }
            else
            {
                rightList.Add(inputList[i]);
            }
        }

        inputList.Clear();

        if (leftList.Count > 0)
        {
            QuickSort_V1(leftList);
            inputList.AddRange(leftList);
        }

        inputList.Add(pivot);

        if (rightList.Count > 0)
        {
            QuickSort_V1(rightList);
            inputList.AddRange(rightList);
        }
    }

    public static void QuickSort_V1_LLM(List<int> inputList)
    {
        if (inputList == null)
            throw new ArgumentNullException(nameof(inputList));

        var serviceProvider = new ServiceCollection()
            .AddTransient<IPivotSelector<int>, FirstElementPivotSelector<int>>()
            .AddTransient<IPartitioner<int>, HoarePartitioner<int>>()
            .AddLogging(logging => logging.AddConsole())
            .BuildServiceProvider();

        var quickSortFactory = new QuickSortFactory<int>(serviceProvider);
        var quickSort = quickSortFactory.CreateQuickSort();

        quickSort.Sort(inputList);
    }

    public static void QuickSort_V2(List<int> inputList, int startIndex = 0, int endIndex = -1)
    {
        endIndex = (endIndex == -1 || endIndex >= inputList.Count) ? (inputList.Count - 1) : endIndex;

        if (endIndex <= startIndex)
        {
            return;
        }

        int pivotIndex = Partition_V1(inputList, startIndex, endIndex);

        QuickSort_V2(inputList, startIndex, pivotIndex - 1);
        QuickSort_V2(inputList, pivotIndex + 1, endIndex);
    }

    /// <summary>
    /// quick sort V3, which is simplified and straightforward.
    /// </summary>
    /// <param name="inputArray"></param>
    /// <param name="left"></param>
    /// <param name="right"></param>
    static void QuickSort_V3(int[] inputArray, int left, int right)
    {
        if (left < right)
        {
            int partitionIndex = Partition_V3(inputArray, left, right);

            QuickSort_V3(inputArray, left, partitionIndex - 1);
            QuickSort_V3(inputArray, partitionIndex + 1, right);
        }
    }

    /// <summary>
    /// Partition the specified collection with the specified range in asc order. The pivot is selected as the first element.
    /// </summary>
    /// <param name="inputList"></param>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <returns>the pivot index</returns>
    /// <exception cref="ArgumentException"></exception>
    private static int Partition_V1(List<int> inputList, int startIndex = 0, int endIndex = -1)
    {
        endIndex = (endIndex == -1 || endIndex >= inputList.Count) ? (inputList.Count - 1) : endIndex;

        if (endIndex <= startIndex)
        {
            throw new ArgumentException("endIndex should be greater than startIndex");
        }        

        int pivot = inputList[startIndex];
        int left = startIndex + 1;
        int right = endIndex;
        while (left < right)
        {
            while (left < endIndex && inputList[left] < pivot)
            {
                left++;
            }

            while (right > startIndex && inputList[right] > pivot)
            {
                right--;
            }            

            // swap left and right
            (inputList[right], inputList[left]) = (inputList[left], inputList[right]);
        }

        // swap left and pivot
        (inputList[startIndex], inputList[left]) = (inputList[left], inputList[startIndex]);

        return left;
    }

    /// <summary>
    /// partition algorithm for quick sort
    /// </summary>
    /// <param name="inputArray"></param>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    static int Partition_V2(int[] inputArray, int left, int right)
    {
        // pick right most element as pivot
        int pivot = inputArray[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (inputArray[j] < pivot)
            {
                i++;

                if(i != j)
                {
                    int temp = inputArray[i];
                    inputArray[i] = inputArray[j];
                    inputArray[j] = temp;
                }
            }
        }

        int temp1 = inputArray[i + 1];
        inputArray[i + 1] = inputArray[right];
        inputArray[right] = temp1;

        return i + 1;
    }

    /// <summary>
    /// partition v2 algorithm for quick sort, which is more straighforward.
    /// </summary>
    /// <param name="inputArray"></param>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    static int Partition_V3(int[] inputArray, int left, int right)
    {
        // select middle element as pivot
        int pivotIndex = left + (right-left)/2;
        int pivot = inputArray[pivotIndex];

        // swap pivot with the first element
        int temp = inputArray[left];
        inputArray[left] = pivot;
        inputArray[pivotIndex] = temp; 

        int i = left + 1;
        int j = right;

        while(true)
        {
            while (i < j && inputArray[i] <= pivot)
            {
                i++;
            }

            while (j > i && inputArray[j] >= pivot)
            {
                j--;
            }

            if(i == j)
            {
                break;
            }

            // swap i and j
            temp = inputArray[i];
            inputArray[i] = inputArray[j];
            inputArray[j] = temp;     
        }

        int newPilotIndex = (inputArray[i] <= pivot) ? i : (i-1);
        temp = inputArray[left];
        inputArray[left] = inputArray[newPilotIndex];
        inputArray[newPilotIndex] = temp;

        return newPilotIndex;
    }

    /// <summary>
    /// Get the top N elements in the specified collection.
    /// </summary>
    /// <param name="inputList"></param>
    /// <param name="topN"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static List<int> GetTopN(List<int> inputList, int topN)
    {
        if (inputList == null)
        {
            throw new ArgumentNullException("inputList is null");
        }

        if (inputList.Count < 1)
        {
            throw new ArgumentException("inputList is empty");
        }

        if (inputList.Count < topN)
        {
            throw new ArgumentException("topN should not be greater than the collection size");
        }

        if(inputList.Count == 1 || inputList.Count == topN)
        {
            return inputList;
        }

        List<int> resultList = new List<int>();
        
        int pivotIndex = Partition_V1(inputList);
        int rightPartCount = inputList.Count - pivotIndex; // including pivot element itself

        var rightList = new List<int>();
        for(int i=pivotIndex;i<inputList.Count;i++)
        {
            rightList.Add(inputList[i]);
        } 

        if(rightPartCount < topN)
        {
            resultList.AddRange(rightList);

            var remainingList = new List<int>();
            for(int i=0;i<pivotIndex;i++)
            {
                remainingList.Add(inputList[i]);
            }

            var newList = GetTopN(remainingList, topN - rightPartCount);
            resultList.AddRange(newList);
        }
        else if(rightPartCount == topN)
        {
            resultList.AddRange(rightList);
        }
        else
        {
            return GetTopN(rightList, topN);
        }

        return resultList;
    }
}