class ArrayTest
{
    /// <summary>
    /// Get the length of the longest asc sub-sequence
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns></returns>
    public static int GetLongestAscSubSequenceCount(int[] inputArray)
    {
        int length = inputArray.Length;
        int maxLength = 0;

        int[] dp = new int[length];
        for(int i=0;i<length;i++)
        {
            dp[i]=1;
            for(int j=0;j<i;j++)
            {
                if(inputArray[j] <= inputArray[i])
                {
                    dp[i] = Math.Max(dp[i], dp[j] + 1);
                }
            }

            if(maxLength < dp[i])
            {
                maxLength = dp[i];
            }
        }

        return maxLength;
    }
}