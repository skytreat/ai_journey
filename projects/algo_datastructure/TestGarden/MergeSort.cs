class MergeSort
{  
    /// <summary>
    /// Merge sort
    /// </summary>
    /// <param name="inputList"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void MergeSort_V1(List<int> inputList)
    {
        if (inputList == null)
        {
            throw new ArgumentNullException("inputList is null");
        }

        if (inputList.Count < 1)
        {
            throw new ArgumentException("inputList is empty");
        }

        // partition
        int middleIndex = inputList.Count / 2;
        List<int> leftList = new List<int>();
        List<int> rightList = new List<int>();
        int i=0;
        for(;i<inputList.Count;i++)
        {
            if(i <= middleIndex)
            {
                leftList.Add(inputList[i]);
            }
            else
            {
                rightList.Add(inputList[i]);
            }
        }

        MergeSort_V1(leftList);
        MergeSort_V1(rightList);

        // merge sort
        int leftLoop = 0, rightLoop = 0;
        while(leftLoop < leftList.Count && rightLoop < rightList.Count)
        {
            if(leftList[leftLoop] <= rightList[rightLoop])
            {
                inputList[i++] = leftList[leftLoop++];                
            }
            else
            {
                inputList[i++] = rightList[rightLoop++];
            }
        }

        if(leftLoop < leftList.Count)
        {
            inputList[i++] = leftList[leftLoop++];  
        }

        if(rightLoop < rightList.Count)
        {
            inputList[i++] = rightList[rightLoop++];  
        }
    }

    /// <summary>
    /// Merge sort v2, which is simplified and straightforward.
    /// </summary>
    /// <param name="inputArray"></param>
    /// <param name="left"></param>
    /// <param name="right"></param>
    static void MergeSort_V2(int[] inputArray, int left, int right)
    {
        if (left < right)
        {
            int middle = left + (right - left) / 2;

            MergeSort_V2(inputArray, left, middle);
            MergeSort_V2(inputArray, middle + 1, right);

            Merge_V2(inputArray, left, middle, right);
        }   
    }   

    /// <summary>
    /// merge algorithm v3 for Merge sort, which is more straightforward.
    /// </summary>
    /// <param name="inputArray"></param>
    /// <param name="left"></param>
    /// <param name="middle"></param>
    /// <param name="right"></param>
    static void Merge_V2(int[] inputArray, int left, int middle, int right)
    {
        int n1 = middle - left + 1;
        int n2 = right - middle;

        int[] leftArray = new int[n1];
        int[] rightArray = new int[n2];

        for (int i = 0; i < n1; i++)
        {
            leftArray[i] = inputArray[left + i];
        }

        for (int j = 0; j < n2; j++)
        {
            rightArray[j] = inputArray[middle + 1 + j];
        }   

        int iIndex = 0, jIndex = 0;
        int k = left;

        while (iIndex < n1 && jIndex < n2)
        {
            if (leftArray[iIndex] <= rightArray[jIndex])
            {
                inputArray[k++] = leftArray[iIndex++];
            }
            else
            {
                inputArray[k++] = rightArray[jIndex++];
            }
        }

        while (iIndex < n1)
        {
            inputArray[k++] = leftArray[iIndex++];
        }

        while (jIndex < n2)
        {
            inputArray[k++] = rightArray[jIndex++];
        }
    }    
}