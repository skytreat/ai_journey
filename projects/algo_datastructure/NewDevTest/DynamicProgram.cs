namespace SkytreatLeetCode
{
    public class DynamicProgram
    {
        public static bool CanPartition(int[] nums)
        {
            int target = 0;
            int maxElement = nums[0];
            int length = nums.Length;
            for (int i = 0; i < length; i++)
            {
                target += nums[i];
                maxElement = Math.Max(maxElement, nums[i]);
            }

            if (target % 2 != 0 
            || maxElement > target / 2)
            {
                // 如果总和是奇数，或者最大元素大于总和的一半，则不可能分成两个子集
                return false;
            }

            target = target / 2;

            // dp[i,j] 表示数组的[0,i]下标范围内是否存在子集和等于j
            // dp[i,j] = dp[i-1,j] || dp[i-1,j-nums[i]]
            var dp = new bool[length, target + 1];
            for (int i = 0; i < length; i++)
            {
                dp[i, 0] = true;//关键点：不选择任何元素，子集和必然等于0
            }

            dp[0, nums[0]] = true;//[0,0]下标范围内必然存在子集和等于nums[0]

            for (int i = 1; i < length; i++)
            {
                for (int j = 1; j <= target; j++)
                {
                    if (dp[i, j])
                    {
                        continue;
                    }

                    if (j < nums[i])
                    {
                        dp[i, j] = dp[i - 1, j];
                    }
                    else
                    {
                        dp[i, j] = dp[i - 1, j] || dp[i - 1, j - nums[i]];
                    }
                }
            }

            return dp[length - 1, target];
        }

        public static bool CanPartition_Optimized(int[] nums)
        {
            int length = nums.Length;
            int targetSum = 0;
            int maxElement = nums[0];
            for (int i = 0; i < length; i++)
            {
                targetSum += nums[i];
                maxElement = Math.Max(maxElement, nums[i]);
            }

            if (targetSum % 2 != 0
                || maxElement > targetSum / 2)
            {
                // 如果总和是奇数，或者最大元素大于总和的一半，则不可能分成两个子集
                return false;
            }

            targetSum = targetSum / 2;

            // solution2: DP array with one dimensions
            // dp[j]表示数组是否存在子集的和等于j
            // dp[j] = dp[j] || dp[j-nums[i]]
            // return dp[targetSum]
            var dp = new bool[targetSum + 1];
            Array.Fill(dp, false);
            dp[0] = true; // 可以凑出和为0的子集！    

            //way2 with single dimension: outer loop with DP index
            for (int j = targetSum; j >= 1; j--)
            {
                if (dp[j])
                {
                    continue;
                }

                for (int i = 0; i < length; i++)
                {
                    if (nums[i] > j)
                    {
                        continue;
                    }

                    dp[j] = dp[j - nums[i]];
                    if (dp[j])
                    {
                        break;
                    }
                }
            }

            return dp[targetSum];
        }

        public static bool WordBreak(string s, IList<string> wordDict)
        {
            // dp[j]表示前j个字符是否可以被词典切分
            // dp[j] = dp[j] || dp[j-word[i]]
            int length = s.Length;
            var dp = new bool[length + 1];
            Array.Fill(dp, false);

            dp[0] = true; // 前0个字符可以被切分

            // way1: outer loop with DP index, it works!
            for (int j = 1; j <= length; j++)
            {
                if (dp[j])
                {
                    continue;
                }

                foreach (var word in wordDict)
                {
                    int wordLength = word.Length;
                    if (j >= wordLength && s.AsSpan(j - wordLength, wordLength).SequenceEqual(word))
                    {
                        dp[j] = dp[j] || dp[j - wordLength];
                    }

                    if (dp[j])
                    {
                        break;
                    }
                }
            }

            // way2: outer loop with item index, it does not work!
            // This approach fails because updating dp[j] in increasing order for each word
            // can cause newly set dp[j] values to be used within the same iteration,
            // leading to overcounting or incorrect propagation of state.
            // In dynamic programming for word break, the DP array should be updated in a way
            // that ensures each state only depends on results from the previous iteration,
            // which is why the outer loop should be on the DP index (j), not on the words.
            /*
            foreach (var word in wordDict)
            {
                int wordLength = word.Length;
                for (int j = wordLength; j <= length; j++)
                {
                    if (s.AsSpan(j - wordLength, wordLength).SequenceEqual(word))
                    {
                        dp[j] = dp[j - wordLength];
                    }

                    //
                    //if (s.Substring(j - wordLength, wordLength) == word)
                    //{
                    //    dp[j] = dp[j - wordLength];
                    //}
                }
            }
            */

            /*
            foreach (var word in wordDict)
            {
                int wordLength = word.Length;
                for (int j = length; j >= wordLength; j--)
                {
                    if (dp[j])
                    {
                        continue;
                    }

                    if (s.AsSpan(j - wordLength, wordLength).SequenceEqual(word))
                    {
                        dp[j] = dp[j - wordLength];
                    }
                }
            }
            */

            return dp[length];
        }
    }
}