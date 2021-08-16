namespace MyHashSet
{
    static class Program
    {
        static void Main(string[] args)
        {
            var hashSet = new MyHashSet<int>();

            hashSet.AddRange(19, 6, 28, 5, 7, 2);
            hashSet.Print();
        }
    }
}