class Number
{
    /// <summary>
    /// Interview (2024/10/29)
    /// </summary>
    /// <param name="number"></param>
    /// <param name="inputArray"></param>
    /// <returns></returns>
    public static int GetMaximumInteger(int number, List<int> inputArray)
    {
        // convert the specified integer to int array first
        List<int> numberArray = NumberToArray(number);        

        // sort the input array in asc order
        inputArray.Sort();

        List<int> resultArray = new List<int>();
        bool isMatched = false;
        for(int i=numberArray.Count - 1;i>=0;i--)
        {
            int currentDigit = numberArray[i];

            if(isMatched)
            {
                resultArray.Add(inputArray[inputArray.Count - 1]);
                continue;
            }

            for(int j=inputArray.Count-1;j>=0;j--)
            {
                if(inputArray[j] > currentDigit)
                {
                    continue;
                }
                else if(inputArray[j] == currentDigit)
                {
                    resultArray.Add(currentDigit);                    
                    break;
                }
                else
                {
                    resultArray.Add(inputArray[j]);
                    isMatched = true;
                    break;
                }
            }            
        }

        // convert the array to int number back
        return ArrayToNumber(resultArray);
    }

    public static List<int> NumberToArray(int number)
    {
        List<int> numberArray = new List<int>();
        do
        {
            numberArray.Add(number % 10);
            number = number / 10;
        } while(number > 0);

        return numberArray;
    }

    public static int ArrayToNumber(List<int> array)
    {
        int resultNumber = array[-1];
        for(int k=array.Count-2;k>=0;k--)
        {
            if(k >= 0)
            {
                resultNumber = resultNumber * 10 + array[k];
            }
        }

        return resultNumber;
    }

    /// <summary>
    /// Get the well-known fibolachy sequence with the given count
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public static int GetClaimStairWayCount(int count)
    {
        int prev_prev = 1;
        int prev = 1;
        int current = 1;
        for(int i=2;i<=count;i++)
        {
            current = prev_prev + prev;
            prev_prev = prev;
            prev = current;            
        }

        return current;
    }

    /// <summary>
    /// Get the maximum full squrt number count, sum of which equals the given integer.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static int GetMaxFullSqurtNumberCount(int n)
    {
        int[] dp = new int[n+1];
        dp[0] = 0;
        for(int i=1;i<=n;i++)
        {
            for(int j=0;j*j <= i;j++)
            {
                dp[i] = Math.Max(dp[i], dp[i-j*j] + 1);
            }
        }

        return dp[n];
    }

    /// <summary>
    /// Get the maximum sum for the non-continuous sub-sequence for the given number array
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns></returns>
    public static int GetMaxSumFromNonContinuousSubSequence(int[] inputArray)
    {
        int arrayLength = inputArray.Length;

        // initialize dp array
        List<int> dp = new List<int>();
        dp.Add(0);
        for(int i=1;i<=arrayLength;i++)
        {
            dp.Add(inputArray[0]);
        }

        for(int j=2;j<=arrayLength;j++)
        {
            dp[j] = Math.Max(dp[j-2] + inputArray[j-1], dp[j-1]);
        }

        return dp[arrayLength];
    }

    /// <summary>
    /// Get all the permutation for the given number array
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns></returns>
    public static List<List<int>> GetPermutation(int[] inputArray)
    {
        List<List<int>> resultList = new List<List<int>>();

        BacktrackForPermutation(inputArray.Length, inputArray, 0, resultList);

        return resultList;
    }

    /// <summary>
    /// Get all the permutation for the given number array
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns></returns>
    public static List<List<int>> GetPermutation(int[] inputArray, int m)
    {
        List<List<int>> resultList = new List<List<int>>();

        BacktrackForPermutation(m, inputArray, 0, resultList);

        return resultList;
    }

    /// <summary>
    /// Get all the combination for the given number array
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns></returns>
    public static List<List<int>> GetCombination(int[] inputArray)
    {
        List<List<int>> resultList = new List<List<int>>();

        List<int> selectedSet = new List<int>();

        BacktrackForCombination(inputArray.Length, inputArray, selectedSet, 0, resultList);

        return resultList;
    }

    /// <summary>
    /// Get all the combination for the given number array
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns></returns>
    public static List<List<int>> GetCombination(int[] inputArray, int m)
    {
        List<List<int>> resultList = new List<List<int>>();

        List<int> selectedSet = new List<int>();

        BacktrackForCombination(m, inputArray, selectedSet, 0, resultList);

        try
        {
            string text = "hello world";
            throw new InvalidOperationException("hello world exception");
        }
        catch(Exception ex)
        {
            throw new InvalidDataException("exception from catch", ex);
        }
        finally
        {
            Console.WriteLine("hello world from finally");
        }

        return resultList;
    }

    /// <summary>
    /// Get the index of the pivot element for the given number array.
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns></returns>
    public static int GetPivotIndex(int[] inputArray)
    {
        int sum = 0;
        foreach(var n in inputArray)
        {
            sum += n;
        }

        int presum = 0;
        for(int i=0;i<inputArray.Length;i++)
        {
            if(2 * presum + inputArray[i] == sum)
            {
                return i;
            }

            presum += inputArray[i];
        }

        return -1;
    }

    /// <summary>
    /// Get sub-array count whose sum equals to the given number
    /// </summary>
    /// <param name="inputArray"></param>
    /// <param name="k"></param>
    /// <returns></returns>
    public static int GetSubArrayCountForSum(int[] inputArray, int k)
    {
        Dictionary<int, int> presumFrequencyMap = new Dictionary<int, int>();

        presumFrequencyMap[0] = 1;

        int presum = 0; // pre-sum
        int result = 0;

        foreach(var n in inputArray)
        {
            presum += n;

            if(presumFrequencyMap.ContainsKey(presum - k))
            {
                result += presumFrequencyMap[presum-k];
            }

            if(presumFrequencyMap.ContainsKey(presum))
            {
                presumFrequencyMap[presum] ++;
            }
            else
            {
                presumFrequencyMap[presum] = 1;
            }
        }

        return result;
    }

    /// <summary>
    /// backtrack for permutation.
    /// currentArray[0..k) is the selected set, currentArray[k..N) is the candidate set.
    /// </summary>
    /// <param name="currentArray"></param>
    /// <param name="k"></param>
    /// <param name="resultList"></param>
    private static void BacktrackForPermutation(int m, int[] inputArray, int k, List<List<int>> resultList)
    {
        // inputArray[0..k) is the selected set, inputArray[k..N) is the candidate set.
        if(k == m)
        {
            var newElement = new List<int>(inputArray);
            resultList.Add(newElement.GetRange(0, m));
            return;
        }

        for(int i=k;i<inputArray.Length;i++)
        {
            // swap i and k first
            Swap(inputArray, k, i);

            // continue resursion from k+1 element
            BacktrackForPermutation(m, inputArray, k+1, resultList);

            // swap i and k back
            Swap(inputArray, k, i);
        }
    }

    /// <summary>
    /// backtrack for combination of the given count
    /// inputArray[k..N) is the candidate set.
    /// </summary>
    /// <param name="m"></param>
    /// <param name="inputArray"></param>
    /// <param name="selectedSet">selected set</param>
    /// <param name="k"></param>
    /// <param name="resultList"></param>
    private static void BacktrackForCombination(int m, int[] inputArray, List<int> selectedSet, int k, List<List<int>> resultList)
    {
        // inputArray[k..N) is the candidate set.
        if(selectedSet.Count == m)
        {
            var newElement = new List<int>(selectedSet);
            resultList.Add(newElement.GetRange(0, m));
            return;
        }

        for(int i=k;i<inputArray.Length;i++)
        {
            // add inputArray[i] to the selected set.
            selectedSet.Add(inputArray[i]);

            // continue resursion from i+1 element since inputArray[k..i] could not be selected anymore.
            BacktrackForCombination(m, inputArray, selectedSet, i+1, resultList);

            // remove inputArray[i] from the selected set.
            selectedSet.RemoveAt(selectedSet.Count - 1);
        }
    }

    private static void Swap(int[] array, int left, int right)
    {
        if( left == right)
        {
            return;
        }

        int temp = array[left];
        array[left]=array[right];
        array[right]=temp;
    }
}