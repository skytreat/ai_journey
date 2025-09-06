namespace SkytreatLeetCode
{
    public class Heaps
    {
        public static int FindKthLargest(int[] nums, int k)
        {
            int length = nums.Length;

            // solution 1
            // construct max heap first
            BuildMaxHeap(nums, length);

            // pop root element (k-1) times
            int heapSize = length;
            for (int i = length - 1; i >= length - k + 1; i--)
            {
                Swap(nums, 0, i);
                --heapSize;
                SinkDown(nums, 0, heapSize);
            }

            return nums[0];

            // solution 2
            // return QuickSelect(nums, 0, length-1, length-k);
        }

        // solution 1: max heap
        private static void BuildMaxHeap(int[] nums, int heapSize)
        {
            for (int i = heapSize / 2 - 1; i >= 0; i--)
            {
                SinkDown(nums, i, heapSize);
            }
        }

        private static void SinkDown(int[] nums, int i, int heapSize)
        {
            // left and right child index
            int l = i * 2 + 1, r = i * 2 + 2;
            int largestIndex = i;

            if (l < heapSize && nums[l] > nums[largestIndex])
            {
                largestIndex = l;
            }

            if (r < heapSize && nums[r] > nums[largestIndex])
            {
                largestIndex = r;
            }

            if (largestIndex != i)
            {
                Swap(nums, largestIndex, i);
                SinkDown(nums, largestIndex, heapSize);
            }
        }

        private static void Swap(int[] nums, int i, int j)
        {
            int tmp = nums[i];
            nums[i] = nums[j];
            nums[j] = tmp;
        }
    }
}