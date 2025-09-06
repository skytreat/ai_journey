using System.Text;

namespace SkytreatLeetCode
{
    public class Strings
    {
        public static string MinWindow(string s, string t)
        {
            int[] charSetArray = new int[70];
            int wordLength = t.Length;
            int strLength = s.Length;
            int minValue = -2 * strLength;
            Array.Fill(charSetArray, minValue);
            for (int i = 0; i < wordLength; i++)
            {
                if (charSetArray[t[i] - 'A'] == minValue)
                {
                    charSetArray[t[i] - 'A'] = 1;
                }
                else
                {
                    charSetArray[t[i] - 'A']++;
                }
            }

            int validCharCount = 0;
            int minLeftIndex = 0, minLength = -1;
            for (int left = 0, right = 0; right < strLength; right++)
            {
                if (charSetArray[s[right] - 'A'] == minValue)
                {
                    // current char does not exist in target str
                    continue;
                }

                if (--charSetArray[s[right] - 'A'] >= 0)
                {
                    validCharCount++;
                }

                while (validCharCount == wordLength)
                {
                    // current sliding windows contains target word

                    // move leftIndex right further to find the shortest substring
                    if (minLength == -1 || minLength > (right - left + 1))
                    {
                        minLength = right - left + 1;
                        minLeftIndex = left;
                    }

                    if (charSetArray[s[left] - 'A'] != minValue && ++charSetArray[s[left] - 'A'] > 0)
                    {
                        --validCharCount;
                    }

                    ++left;
                }
            }

            return minLength == -1 ? "" : s.Substring(minLeftIndex, minLength);
        }

        public static IList<int> FindAnagrams(string s, string p)
        {
            var resultList = new List<int>();
            int wordLength = p.Length;

            if (s.Length < wordLength)
            {
                return resultList;
            }

            var countArray = new int[26];
            Array.Fill(countArray, 0);
            for (int i = 0; i < wordLength; i++)
            {
                countArray[p[i] - 'a']++;
            }

            for (int l = 0, r = 0; r < s.Length; r++)
            {
                // add the next right element 
                countArray[s[r] - 'a']--;
                while (countArray[s[r] - 'a'] < 0)
                {
                    // forward left pointer
                    countArray[s[l] - 'a']++;
                    l++;
                }

                if (r - l + 1 == wordLength)
                {
                    resultList.Add(l);
                }
            }

            return resultList;
        }

        private static bool AreAnagrams(string s1, string s2)
        {
            var hashArray = new int[26];
            Array.Fill(hashArray, 0);

            foreach (var c in s1)
            {
                hashArray[c - 'a']++;
            }

            foreach (var c in s2)
            {
                hashArray[c - 'a']--;
            }

            foreach (var i in hashArray)
            {
                if (i != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static int LengthOfLongestSubstring(string s)
        {
            if (s.Length == 0)
            {
                return 0;
            }

            int maxSubArrayLength = 1;

            // solution1: brute-force
            for (int i = 0; i < s.Length; i++)
            {
                var charSet = new HashSet<char> { s[i] };
                for (int j = i + 1; j < s.Length; j++)
                {
                    if (charSet.Contains(s[j]))
                    {
                        break;
                    }

                    charSet.Add(s[j]);
                    maxSubArrayLength = Math.Max(maxSubArrayLength, j - i + 1);
                }
            }

            return maxSubArrayLength;
        }

        public static string DecodeString(string s)
        {
            int length = s.Length;
            int i = 0;
            var stack = new Stack<string>();
            string result = "";
            while (i < length)
            {
                if (s[i] >= '0' && s[i] <= '9')
                {
                    var number = GetDigits(s, i);
                    i = i + number.Length;
                    stack.Push(number);
                }
                else if (s[i] == '[' || (s[i] >= 'a' && s[i] <= 'z'))
                {
                    stack.Push(s[i].ToString());
                    i++;
                }
                else
                {
                    // s[i] == ']'                
                    string token = "";
                    while (stack.Peek() != "[")
                    {
                        token = stack.Peek() + token;
                        stack.Pop();
                    }

                    stack.Pop(); // pop '['

                    var repeatText = stack.Peek();
                    stack.Pop();
                    int repeatTimes = StringToInt(repeatText); // pop repeat number
                    StringBuilder tmpResult = new StringBuilder();
                    for (int j = 0; j < repeatTimes; j++)
                    {
                        tmpResult.Append(token);
                    }

                    stack.Push(tmpResult.ToString());
                    i++;
                }
            }

            while (stack.Count > 0)
            {
                var token = stack.Peek();
                stack.Pop();
                result = token + result;
            }

            return result;
        }

        private static int StringToInt(string s)
        {
            int length = s.Length;
            int result = 0;
            for (int i = 0; i < length; i++)
            {
                result = result * 10 + (s[i] - '0');
            }

            return result;
        }

        private static string GetDigits(string s, int startIndex)
        {
            // get the digits from s[startIndex]
            int charCount = 0;
            while (s[startIndex + charCount] >= '0' && s[startIndex + charCount] <= '9')
            {
                charCount++;
            }

            return s.Substring(startIndex, charCount);
        }
    }
}