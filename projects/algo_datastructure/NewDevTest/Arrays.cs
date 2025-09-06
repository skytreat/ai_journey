namespace SkytreatLeetCode
{
    public class Arrays
    {
        /// <summary>
        /// Given an array of intervals where intervals[i] = [starti, endi], merge all overlapping intervals, and return an array of the non-overlapping intervals that cover all the intervals in the input.
        /// </summary>
        /// <param name="intervals"></param>
        /// <returns></returns>
        public static int[][] MergeIntervals(int[][] intervals)
        {
            var resultList = new List<int[]>();

            if (intervals.Length == 0)
            {
                return resultList.ToArray();
            }

            Array.Sort(intervals, (l, r) => l[0].CompareTo(r[0]));

            int[] currentInterval = new int[2] { intervals[0][0], intervals[0][1] };
            for (int i = 1; i < intervals.Length; i++)
            {
                if (currentInterval[1] >= intervals[i][0])
                {
                    // current interval is overlap with the next interval
                    currentInterval[1] = intervals[i][1];
                }
                else
                {
                    // current interval is not overlap with the next interval
                    resultList.Add(currentInterval);
                    currentInterval = new int[2] { intervals[i][0], intervals[i][1] };
                }
            }

            resultList.Add(currentInterval);
            return resultList.ToArray();
        }

        public static int FirstMissingPositive(int[] nums)
        {
            // solution1: get the min and max from the given array
            /*
            int minValue = int.MaxValue;
            int maxValue = int.MinValue;
            var hashSet = new HashSet<int>();

            foreach(var num in nums) {
                if(num <= 0) {
                    continue;
                }

                hashSet.Add(num);

                minValue = Math.Min(minValue, num);
                maxValue = Math.Max(maxValue, num);
            }

            if(minValue > 1) {
                return 1;
            }

            int i=1;
            while(i <= maxValue) {
                if(!hashSet.Contains(i)) {
                    break;
                } 

                i++;
            }

            return i;
            */

            // solution2: array -> hash table
            int length = nums.Length;
            int i=0;
            for(i=0;i<length;i++) {
                if(nums[i] <= 0) {
                    nums[i] = length + 1;
                }
            }

            for(i=0;i<length;i++) {
                int num = Math.Abs(nums[i]);
                if(num <= length) {
                    nums[num-1] = -1 * Math.Abs(nums[num-1]);
                }
            }

            for(i=0;i<length;i++) {
                if(nums[i] > 0) {
                    return i+1;
                }
            }

            return length+1;
        }

        /// <summary>
        /// Given an integer array nums, return all the unique triplets {nums[i], nums[j], nums[k]} such that i != j != k, and nums[i] + nums[j] + nums[k] == 0.
        /// The result must not contain duplicate triplets.
        /// </summary>
        public static IList<IList<int>> ThreeSum(int[] nums)
        {
            // order first and then two pointers
            var resultList = new List<IList<int>>();

            // sort the array first
            Array.Sort(nums);

            // key: array element, value: indexes of the element in the array
            var dictionary = new Dictionary<int, List<int>>();
            int minPositiveIndex = nums.Length;
            for (int z = 0; z < nums.Length; z++)
            {
                if (nums[z] >= 0)
                {
                    minPositiveIndex = Math.Min(minPositiveIndex, z);
                }

                if (dictionary.ContainsKey(nums[z]))
                {
                    dictionary[nums[z]].Add(z);
                }
                else
                {
                    dictionary[nums[z]] = new List<int> { z };
                }
            }

            var sumSet = new HashSet<int>();
            for (int i = 0; i < minPositiveIndex; i++)
            {
                // two pointers: i points to negative number, k points to positive number
                for (int k = nums.Length - 1; k >= minPositiveIndex; k--)
                {
                    int currentSum = nums[i] + nums[k];
                    var targetElement = -1 * currentSum;
                    if (dictionary.ContainsKey(targetElement) && !sumSet.Contains(targetElement))
                    {
                        List<int> indexes = dictionary[targetElement];
                        foreach (var index in indexes)
                        {
                            if (index > i && index < k)
                            {
                                sumSet.Add(targetElement);
                                resultList.Add(new List<int> { nums[i], nums[index], nums[k] });
                                break;
                            }
                        }
                    }
                }
            }

            return resultList;
        }

        /// <summary>
        /// Given an integer array nums, return all the unique triplets {nums[i], nums [j], nums[k]} such that i != j != k, and nums[i] + nums[j] + nums[k] == 0.
        /// The result must not contain duplicate triplets.
        /// This is a solution from Copilot.
        /// </summary>
        public static IList<IList<int>> ThreeSum_FromCopilot(int[] nums)
        {
            var resultList = new List<IList<int>>();
            Array.Sort(nums);

            for (int i = 0; i < nums.Length - 2; i++)
            {
                if (i > 0 && nums[i] == nums[i - 1])
                {
                    continue; // skip duplicates
                }

                // Use two pointers to find the other two numbers
                // that sum with nums[i] to zero
                int left = i + 1, right = nums.Length - 1;
                while (left < right)
                {
                    int sum = nums[i] + nums[left] + nums[right];
                    if (sum == 0)
                    {
                        resultList.Add(new List<int> { nums[i], nums[left], nums[right] });
                        while (left < right && nums[left] == nums[left + 1]) left++; // skip duplicates
                        while (left < right && nums[right] == nums[right - 1]) right--; // skip duplicates
                        left++;
                        right--;
                    }
                    else if (sum < 0)
                    {
                        left++;
                    }
                    else
                    {
                        right--;
                    }
                }
            }

            return resultList;
        }
    }
}