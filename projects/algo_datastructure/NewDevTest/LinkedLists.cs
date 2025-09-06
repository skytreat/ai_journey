namespace SkytreatLeetCode
{
    public class ListNode
    {
        public int val;
        public ListNode next;
        public ListNode(int val = 0, ListNode next = null)
        {
            this.val = val;
            this.next = next;
        }
    }

    public class LinkedLists
    {
        public static bool IsPalindrome_FromCopilot(ListNode head)
        {
            // 1. find the middle node of linked list
            // 2. reverse the second half of linked list
            // 3. compare the first half and the second half

            if (head == null || head.next == null)
            {
                return true;
            }

            ListNode slow = head, fast = head;
            while (fast != null && fast.next != null)
            {
                slow = slow.next;
                fast = fast.next.next;
            }

            // reverse the linked list with range: [slow,fast]      
            ListNode prev = null, current = slow;
            while (current != null)
            {
                ListNode next = current.next;
                current.next = prev;
                prev = current;
                current = next;
            }

            ListNode n1 = head, n2 = prev;
            while (n1 != null && n2 != null)
            {
                if (n1.val != n2.val)
                {
                    return false;
                }

                n1 = n1.next;
                n2 = n2.next;
            }

            return (n1 == null && n2 == null);
        }

        public static bool IsPalindrome(ListNode head)
        {
            ListNode slow = head, fast = head;
            while (fast.next != null)
            {
                slow = slow.next;
                fast = fast.next;
                if (fast.next != null)
                {
                    fast = fast.next;
                }
            }

            // reverse the linked list with range: [slow,fast]      
            ListNode prev = null, current = slow;
            while (current != null)
            {
                ListNode next = current.next;
                current.next = prev;
                prev = current;
                current = next;
            }

            ListNode n1 = head, n2 = prev;
            while (n2 != null)
            {
                if (n1.val != n2.val)
                {
                    return false;
                }

                n1 = n1.next;
                n2 = n2.next;
            }

            return true;
        }
    }
}