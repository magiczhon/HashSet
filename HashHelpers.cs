using System;

namespace MyHashSet
{
    internal static class HashHelpers
    {
        internal static readonly int[] Primes = {
            3,
            7,
            11,
            17,
            23,
            29,
            37,
            47,
            59,
            71,
            89,
            107,
            131,
            163,
            197,
            239,
            293,
            353,
            431,
            521,
            631,
            761,
            919,
            1103,
            1327,
            1597,
            1931,
            2333,
            2801,
            3371,
            4049,
            4861,
            5839,
            7013,
            8419,
            10103,
            12143,
            14591,
            17519,
            21023,
            25229,
            30293,
            36353,
            43627,
            52361,
            62851,
            75431,
            90523,
            108631,
            130363,
            156437,
            187751,
            225307,
            270371,
            324449,
            389357,
            467237,
            560689,
            672827,
            807403,
            968897,
            1162687,
            1395263,
            1674319,
            2009191,
            2411033,
            2893249,
            3471899,
            4166287,
            4999559,
            5999471,
            7199369
        };

        internal static bool IsPrime(int candidate)
        {
            if ((candidate & 1) == 0)
                return candidate == 2;
            int num1 = (int)Math.Sqrt(candidate);
            int num2 = 3;
            while (num2 <= num1)
            {
                if (candidate % num2 == 0)
                    return false;
                num2 += 2;
            }
            return true;
        }

        internal static int GetPrime(int min)
        {
            for (int index = 0; index < Primes.Length; ++index)
            {
                int num = Primes[index];
                if (num >= min)
                    return num;
            }
            int candidate = min | 1;
            while (candidate < int.MaxValue)
            {
                if (IsPrime(candidate))
                    return candidate;
                candidate += 2;
            }
            return min;
        }

        internal static int GetMinPrime()
        {
            return Primes[0];
        }
    }
}