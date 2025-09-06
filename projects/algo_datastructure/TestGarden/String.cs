class String
{
    /// <summary>
    /// Get the first unique character in the given string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static char GetFirstUniqueChar(string input)
    {
        List<int> array = new List<int>();
        for(int i=0;i<128;i++)
        {
            array.Add(0);
        }

        foreach(var c in input)
        {
            array[c - '0'] += 1;
        }

        foreach(var d in input)
        {
            if(array[d - '0'] == 1)
            {
                return d;
            }
        }

        return '#';
    }

    /// <summary>
    /// Check whether the given string could be split by the given word dictionary
    /// </summary>
    /// <param name="inputString"></param>
    /// <param name="dictionary"></param>
    /// <returns></returns>
    public static bool CanBreakWord(string inputString, List<string> dictionary)
    {
        int length = inputString.Length;
        bool[] dp = new bool[length+1];
        dp[0] = true;

        for(int i=1;i<=length;i++)
        {
            dp[i] = false;
            foreach(var word in dictionary)
            {
                int wordLength = word.Length;
                if(i >= wordLength && inputString.Substring(i - wordLength, wordLength) == word)
                {
                    dp[i] = dp[i - wordLength];
                }

                if(dp[i])
                {
                    break;
                }
            }            
        }

        return dp[length];
    }

    /// <summary>
    /// Get the length for the longest common sequence
    /// </summary>
    /// <param name="leftString"></param>
    /// <param name="rightString"></param>
    /// <returns></returns>
    public static int GetMaxLengthOfLCS(string leftString, string rightString)
    {
        int leftLength = leftString.Length;
        int rightLength = rightString.Length;

        int[,] dp = new int[leftLength+1, rightLength+1];
        dp[0,0] = 0;
        dp[0,1] = 0;
        dp[1,0] = 0;

        for(int i=1;i<=leftLength;i++)
        {            
            for(int j=1;j<= rightLength;j++)
            {
                dp[i,j] = 0;
                if(leftString[i-1] == rightString[j-1])
                {
                    dp[i,j] = dp[i-1,j-1] + 1;
                }
                else 
                {
                    dp[i,j] = Math.Max(dp[i-1,j], dp[i,j-1]);
                }
            }
        }

        return dp[leftLength, rightLength];
    }

    /// <summary>
    /// A-Z matches to 1-26, get the total count of decoding ways for the given string in 1-26
    /// For example, 226 -> BBF(2,2,6), BZ(2,26), VF(22,6)
    /// </summary>
    /// <param name="inputString"></param>
    /// <returns></returns>
    public static int GetDecodeWayTotalCount(string inputString)
    {
        int length = inputString.Length;

        int prev = inputString[0] - '0';
        if(prev == 0)
        {
            return 0;
        }

        if(length == 1)
        {
            return 1;
        }

        int[] dp = new int[length+1];
        for(int i=2;i<=length;i++)
        {
            dp[i] = 1;

            int current = inputString[i-1] - '0';
            if((prev == 0 || prev > 2) && current == 0)
            {
                return 0;
            }

            if((prev == 1) || (prev == 2 && current <= 6))
            {
                // in range [10, 26]
                if(current == 0)
                {
                    dp[i] = dp[i-2];
                }
                else
                {
                    dp[i] = dp[i-2] + dp[i-1];
                }
            }
            else
            {
                dp[i] = dp[i-1];
            }

            prev = current;
        }

        return dp[length];
    }    
}
