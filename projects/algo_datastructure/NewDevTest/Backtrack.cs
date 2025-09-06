namespace SkytreatLeetCode
{
    public class Backtrack
    {
        public static bool ExistsWord(char[][] board, string word)
        {
            bool[,] visited = new bool[board.Length,board[0].Length];
            for(int i=0; i < board.Length; i++)
            {
                for(int j=0; j < board[0].Length; j++)
                {
                    if (Backtrack_ExistsWord(board, word, visited, i, j, 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static List<IList<int>> SubsetSumWithUniqueElements(int[] nums, int targetSum)
        {
            if (nums.Length == 0 || targetSum < 0)
            {
                return new List<IList<int>>();
            }

            Array.Sort(nums); // Sort to handle duplicates and ensure unique combinations

            var resultList = new List<IList<int>>();
            Backtrack_SubsetSumWithUniqueElements(targetSum, nums, 0, new List<int>(), resultList);
            return resultList;
        }

        private static bool Backtrack_ExistsWord(char[][] board, string word, bool[,] visited, int r, int c, int wordIndex)
        {
            if (r < 0 || r >= board.Length
                || c < 0 || c >= board[0].Length
                || visited[r, c]
                || board[r][c] != word[wordIndex])
            {
                return false;
            }

            if (wordIndex == (word.Length - 1))
            {
                return true;
            }

            visited[r, c] = true;
            var found = Backtrack_ExistsWord(board, word, visited, r, c + 1, wordIndex + 1)
            || Backtrack_ExistsWord(board, word, visited, r, c - 1, wordIndex + 1)
            || Backtrack_ExistsWord(board, word, visited, r + 1, c, wordIndex + 1)
            || Backtrack_ExistsWord(board, word, visited, r - 1, c, wordIndex + 1);

            visited[r, c] = false;
            return found;
        }

        private static void Backtrack_SubsetSumWithUniqueElements(int targetSum, int[] nums, int startIndex, List<int> currentSection, List<IList<int>> resultList)
        {
            if (targetSum == 0)
            {
                resultList.Add(new List<int>(currentSection));
                return;
            }

            if (targetSum < 0)
            {
                // If the target sum is negative, no need to continue
                return;
            }

            for (int i = startIndex; i < nums.Length; i++)
            {
                if (nums[i] > targetSum)
                {
                    break;
                }

                // make the choice
                currentSection.Add(nums[i]);

                // continue to the next index
                Backtrack_SubsetSumWithUniqueElements(targetSum - nums[i], nums, i, currentSection, resultList);

                // undo choice
                currentSection.RemoveAt(currentSection.Count - 1);
            }
        }

        public static List<IList<int>> SubsetSumWithDuplicateElements(int[] nums, int targetSum)
        {
            if (nums.Length == 0 || targetSum < 0)
            {
                return new List<IList<int>>();
            }

            Array.Sort(nums); // Sort to handle duplicates and ensure unique combinations

            var resultList = new List<IList<int>>();
            Backtrack_SubsetSumWithDuplicateElements(targetSum, nums, 0, new List<int>(), resultList);
            return resultList;
        }

        private static void Backtrack_SubsetSumWithDuplicateElements(int targetSum, int[] nums, int startIndex, List<int> currentSection, List<IList<int>> resultList)
        {
            if (targetSum == 0)
            {
                resultList.Add(new List<int>(currentSection));
                return;
            }

            if (targetSum < 0)
            {
                // If the target sum is negative, no need to continue
                return;
            }

            for (int i = startIndex; i < nums.Length; i++)
            {
                if (i > startIndex && nums[i] == nums[i - 1])
                {
                    // Skip duplicates
                    continue;
                }

                if (nums[i] > targetSum)
                {
                    break;
                }

                // make the choice
                currentSection.Add(nums[i]);

                // continue to the next index
                Backtrack_SubsetSumWithUniqueElements(targetSum - nums[i], nums, i + 1, currentSection, resultList);

                // undo choice
                currentSection.RemoveAt(currentSection.Count - 1);
            }
        }

        public static IList<IList<int>> Permute(int[] nums)
        {
            var resultList = new List<IList<int>>();

            Console.WriteLine("About to start permutation backtracking");
            Backtrack_Permute(0, nums, resultList);

            return resultList;
        }

        /// <summary>
        /// Backtracking Permutation
        /// </summary>
        /// <param name="index">the index to start permutation</param>
        /// <param name="nums">the array to be permuted</param>
        /// <param name="resultList">the list to store all the unique permutations</param>
        private static void Backtrack_Permute(int index, int[] nums, IList<IList<int>> resultList)
        {
            int length = nums.Length;
            if (index == length - 1)
            {
                resultList.Add(new List<int>(nums));
                return;
            }

            for (int i = index; i < length; i++)
            {
                // make the choice, swap i and index
                if (i != index)
                {
                    // swap nums[i] and nums[index]
                    int temp = nums[i];
                    nums[i] = nums[index];
                    nums[index] = temp;
                }
                else
                {
                    // no need to swap
                    Console.WriteLine($"No swap: {string.Join(",", nums.Take(index + 1))}");
                }

                Console.WriteLine($"Before recursion: {string.Join(",", nums.Take(index + 1))}");

                // continue to the next index
                // this is the key step to continue the permutation
                Backtrack_Permute(index + 1, nums, resultList);

                // undo choice, swap back i and index
                if (i != index)
                {
                    // swap nums[i] and nums[index] back
                    int temp = nums[i];
                    nums[i] = nums[index];
                    nums[index] = temp;
                }
                else
                {
                    // no need to swap back
                    Console.WriteLine($"No swap back: {string.Join(",", nums.Take(index + 1))}");
                }

                Console.WriteLine($"After recursion: {string.Join(",", nums.Take(index + 1))}");
            }
        }
    }
}