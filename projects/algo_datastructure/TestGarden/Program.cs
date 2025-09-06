// See https://aka.ms/new-console-template for more information
using System.Collections;

Console.WriteLine("Hello, World!");

// string myFirstText = "hello world again";

// var inputString = "abdesacb";
// var firstUniqueChar = String.GetFirstUniqueChar(inputString);
// Console.WriteLine($"The first unique char for {inputString} is {firstUniqueChar}");

// static void PrintStringCollection(IEnumerable<string> list)
// {
//     Console.WriteLine("[");
//     foreach(var element in list)
//     {
//         Console.Write(element + ",");
//     }

//     Console.WriteLine("]");
// }

// static void PrintCollection(IEnumerable<int> list)
// {
//     Console.WriteLine("[" + string.Join(", ", list) + "]");
// }

// int resultNumber = Number.GetMaximumInteger(23121, new List<int>{2, 4, 9});
// Console.WriteLine(resultNumber);

// Console.WriteLine(myFirstText);

// List<int> list1 = new List<int> {3, 5, 9,2, 5, 11,0, 8,7};
// QuickSort.QuickSort_V1(list1);
// PrintCollection(list1);

// list1 = new List<int> {3, 3, 9,2, 5, 11,0, 8,3};
// QuickSort.QuickSort_V1(list1);
// PrintCollection(list1);

// System.Console.WriteLine("Top 3 list is: ");
// var topNList = QuickSort.GetTopN(list1, 3);
// PrintCollection(topNList);

// List<int> list2 = new List<int> {3, 5, 2};
// QuickSort.QuickSort_V2(list2);
// PrintCollection(list2);

// List<int> list3 = new List<int> {3};
// QuickSort.QuickSort_V2(list3);
// PrintCollection(list3);

// System.Console.WriteLine("Top 2 list is: ");
// topNList = QuickSort.GetTopN(list3, 2);
// PrintCollection(topNList);

// int[] inputArray1 = new int[] {2, 9, 8, 3, 6};
// Console.WriteLine(Math.Equals(16, Number.GetMaxSumFromNonContinuousSubSequence(inputArray1)));

// int[] inputArray2 = new int[] {10, 1, 3, 9};
// Console.WriteLine(Math.Equals(16, Number.GetMaxSumFromNonContinuousSubSequence(inputArray2)));
// Console.WriteLine(Math.Equals(19, Number.GetMaxSumFromNonContinuousSubSequence(inputArray2)));

int[] inputArrayForPermutation = new int[]{1,3,5,7};
var permutationList = Number.GetPermutation(inputArrayForPermutation, 3);

Console.WriteLine($"There are {permutationList.Count} permutation for the given array: " + System.String.Join( ",", inputArrayForPermutation));
foreach(var element in permutationList)
{
    Console.WriteLine(System.String.Join( ",", element));
}

var combinationList = Number.GetCombination(inputArrayForPermutation, 3);

Console.WriteLine($"There are {combinationList.Count} combination for the given array: " + System.String.Join( ",", inputArrayForPermutation));
foreach(var element in combinationList)
{
    Console.WriteLine(System.String.Join( ",", element));
}
