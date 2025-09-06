namespace SkytreatLeetCode
{
    public class Monotonous
    {
        public static int[] MaxSlidingWindow(int[] nums, int k)
        {
            int length = nums.Length;
            var result = new int[length - k + 1];

            // solution1: use max-heap
            /*
            var maxHeap = new PriorityQueue<int,int>();
            for(int i=0;i<k;i++) {
                maxHeap.Enqueue(i, -1 * nums[i]);
            }

            result[0] = nums[maxHeap.Peek()];
            for(int i=k;i<length;i++) {
                maxHeap.Enqueue(i, -1 * nums[i]);

                while(maxHeap.Peek() <= (i-k)) {
                    maxHeap.Dequeue();
                }

                result[i-k+1] = nums[maxHeap.Peek()];
            }
            */

            //solution2: 使用单调队列，队列是递减的，队首最大，队尾最小
            var deque = new LinkedList<int>();
            for (int i = 0; i < length; i++)
            {
                while (deque.Count > 0 && deque.Last != null && nums[deque.Last.Value] <= nums[i])
                {
                    // remove all elements which are less than current element
                    // because they will not be the max in the next k windows
                    deque.RemoveLast();
                }

                deque.AddLast(i);

                if (deque.First != null && deque.First.Value <= (i - k))
                {
                    // remove the first element if it is out of the current window
                    // because it will not be the max in the next k windows
                    deque.RemoveFirst();
                }

                if (i + 1 >= k)
                {
                    if (deque.First != null)
                    {
                        result[i + 1 - k] = nums[deque.First.Value];
                    }
                }
            }


            return result;
        }
    }
}