using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxLib.General
{
    /// <summary>
    /// Static methods for combining hash codes, used during the implementation of
    /// value-type objects.  See 
    /// http://musingmarc.blogspot.com/2007/08/vtos-rtos-and-gethashcode-oh-my.html 
    /// and http://musingmarc.blogspot.com/2008/03/sometimes-you-make-hash-of-things.html .
    /// </summary>
    public static class CombineHashCodes
    {
        /// <summary>
        /// Returns a hash code combining the hash codes supplied as parameters.
        /// </summary>
        public static int Combine(params int[] hashes)
        {
            int hash = 0;

            for (int index = 0; index < hashes.Length; index++)
            {
                hash = (hash << 5) + hash;
                hash ^= hashes[index];
            }

            return hash;
        }

        private static int GetEntryHash(object entry)
        {
            int entryHash = 0x61E04917; // slurped from .Net runtime internals...

            if (entry != null)
            {
                object[] subObjects = entry as object[];

                if (subObjects != null)
                {
                    entryHash = Combine(subObjects);
                }
                else
                {
                    entryHash = entry.GetHashCode();
                }
            }

            return entryHash;
        }

        /// <summary>
        /// Returns a hash code combining the hash codes of the objects supplied as parameters.
        /// </summary>
        public static int Combine(params object[] objects)
        {
            return Combine(objects as IEnumerable<object>);
        }

        /// <summary>
        /// Returns a hash code combining the hash codes of the objects supplied as a sequence.
        /// </summary>
        public static int Combine<T>(IEnumerable<T> objects)
        {
            int hash = 0;

            foreach (object obj in objects)
            {
                hash = (hash << 5) + hash;
                hash ^= GetEntryHash(obj);
            }

            return hash;
        }

        /// <summary>
        /// Returns a hash code combining the hash codes supplied as parameters.
        /// </summary>
        public static int Combine(int hash1, int hash2)
        {
            return ((hash1 << 5) + hash1)
                   ^ hash2;
        }

        /// <summary>
        /// Returns a hash code combining the hash codes supplied as parameters.
        /// </summary>
        public static int Combine(int hash1, int hash2, int hash3)
        {
            int hash = Combine(hash1, hash2);
            return ((hash << 5) + hash)
                   ^ hash3;
        }

        /// <summary>
        /// Returns a hash code combining the hash codes supplied as parameters.
        /// </summary>
        public static int Combine(int hash1, int hash2, int hash3, int hash4)
        {
            int hash = Combine(hash1, hash2, hash3);
            return ((hash << 5) + hash)
                   ^ hash4;
        }

        /// <summary>
        /// Returns a hash code combining the hash codes supplied as parameters.
        /// </summary>
        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5)
        {
            int hash = Combine(hash1, hash2, hash3, hash4);
            return ((hash << 5) + hash)
                   ^ hash5;
        }

        /// <summary>
        /// Returns a hash code combining the hash codes of the objects supplied as parameters.
        /// </summary>
        public static int Combine(object obj1, object obj2)
        {
            return Combine(obj1.GetHashCode()
                , obj2.GetHashCode());
        }

        /// <summary>
        /// Returns a hash code combining the hash codes of the objects supplied as parameters.
        /// </summary>
        public static int Combine(object obj1, object obj2, object obj3)
        {
            return Combine(obj1.GetHashCode()
                , obj2.GetHashCode()
                , obj3.GetHashCode());
        }

        /// <summary>
        /// Returns a hash code combining the hash codes of the objects supplied as parameters.
        /// </summary>
        public static int Combine(object obj1, object obj2, object obj3, object obj4)
        {
            return Combine(obj1.GetHashCode()
                , obj2.GetHashCode()
                , obj3.GetHashCode()
                , obj4.GetHashCode());
        }

        /// <summary>
        /// Returns a hash code combining the hash codes of the objects supplied as parameters.
        /// </summary>
        public static int Combine(object obj1, object obj2, object obj3, object obj4, object obj5)
        {
            return Combine(obj1.GetHashCode()
                , obj2.GetHashCode()
                , obj3.GetHashCode()
                , obj4.GetHashCode()
                , obj5.GetHashCode());
        }
    }
}