// See https://aka.ms/new-console-template for more information

/*
给定一个单词数组 words 和一个长度 maxWidth ，重新排版单词，使其成为每行恰好有 maxWidth 个字符，且左右两端对齐的文本。
尽可能多地往每行中放置单词，必要时可用空格 ' ' 填充，使得每行恰好有 maxWidth 个字符。
要求尽可能均匀分配单词间的空格数量。如果某一行单词间的空格不能均匀分配，则左侧放置的空格数要多于右侧的空格数。
文本的最后一行应为左对齐，且单词之间不插入额外的空格。
注意:
单词是指由非空格字符组成的字符序列。
每个单词的长度大于 0，小于等于 maxWidth。
输入单词数组 words 至少包含一个单词。
 
示例 1:
输入: words = ["This", "is", "an", "example", "of", "text", "justification."], maxWidth = 16
输出:
[
   "This    is    an",
   "example  of text",
   "justification.  "
]

示例 2:
输入:words = ["What","must","be","acknowledgment","shall","be"], maxWidth = 16
输出:
[
  "What   must   be",
  "acknowledgment  ",
  "shall be        "
]
解释: 注意最后一行的格式应为 "shall be    " 而不是 "shall     be",
     因为最后一行应为左对齐，而不是左右两端对齐。       
     第二行同样为左对齐，这是因为这行只包含一个单词。

示例 3:
输入:words = ["Science","is","what","we","understand","well","enough","to","explain","to","a","computer.","Art","is","everything","else","we","do"]，maxWidth = 20
输出:
[
  "Science  is  what we",
  "understand      well",
  "enough to explain to",
  "a  computer.  Art is",
  "everything  else  we",
  "do                  "
]

 
提示:
1 <= words.length <= 300
1 <= words[i].length <= 20
words[i] 由小写英文字母和符号组成
1 <= maxWidth <= 100
words[i].length <= maxWidth
*/

using System.Text;

static List<string> GetListString(int maxWidth, List<string> inputWordList)
{
    var resultList = new List<string>();

    int charCounter = 0;
    List<string> currentLineList = new List<string>();
    foreach(var word in inputWordList)
    {
        if(charCounter + word.Length > maxWidth)
        {
            resultList.Add(AdjustStringList(currentLineList, maxWidth));
            
            // new line
            currentLineList = [word];
            charCounter = word.Length + 1;
        }
        else
        {
            charCounter += word.Length + 1;
            currentLineList.Add(word);
        }
    }

    // the last line
    if(currentLineList.Count > 0)
    {
        resultList.Add(AdjustStringList(currentLineList, maxWidth));
    }

    return resultList;
}

///
/// further process each lines to adjust space position
static string AdjustStringList(List<string> inputList, int maxWidth)
{
    int wordCount = inputList.Count;
    if(wordCount <= 1)
    {
        return inputList[0];
    }

    int charCount = 0;
    foreach(var word in inputList)
    {
        charCount += word.Length;
    }

    int totalSpaceCount = maxWidth - charCount;

    int maxIntervalSpaceCount = totalSpaceCount / (wordCount - 1);
    if(totalSpaceCount % (wordCount - 1) > 0)
    {
        maxIntervalSpaceCount += 1;
    }

    var resultString = new StringBuilder();
    // int wordCounter = 1;
    int totalSpaceCounter = 0;
    foreach(var word in inputList)
    {
        resultString.Append(word);
        for(int j=0;j<maxIntervalSpaceCount && totalSpaceCounter < totalSpaceCount;j++, totalSpaceCounter++)
        {
            resultString.Append(" ");
        }
    }

    return resultString.ToString();
}

var resultList = GetListString(20, new List<string>{"Science","is","what","we","understand","well","enough","to","explain","to","a","computer.","Art","is","everything","else","we","do"});
foreach(var ele in resultList)
{
    Console.WriteLine(ele);
}