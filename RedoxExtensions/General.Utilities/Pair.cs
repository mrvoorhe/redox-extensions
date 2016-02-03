using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.General.Utilities
{
    /// <summary>
    /// A simple class containing two objects.  Pairs can be compared for equality and stored in
    /// dictionaries.
    /// </summary>
    /// <remarks>An equivalent C++ type would also support cross-casting to pairs of related types,
    /// but I don't know how to do that in C#.</remarks>
    
    public class Pair<T1, T2>
    {
        /// <summary>
        /// The first item in the pair.
        /// </summary>
        
        public T1 Item1;

        /// <summary>
        /// The second item in the pair.
        /// </summary>
        
        public T2 Item2;

        /// <summary>
        /// Constructs a pair initialised with the given items.
        /// </summary>
        
        public Pair(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        
        public Pair(Pair<T1, T2> rhs)
        {
            this.Item1 = rhs.Item1;
            this.Item2 = rhs.Item2;
        }

        /// <summary>
        /// Construct an empty pair.
        /// </summary>
        
        public Pair()
        {
        }

        /// <summary>
        /// Test equality.
        /// </summary>
        
        public override bool Equals(object obj)
        {
            Pair<T1, T2> rhs = obj as Pair<T1, T2>;

            if (rhs != null)
            {
                return this.Equals(rhs);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        /// <summary>
        /// Overloaded equality test explicitly taking pairs.
        /// </summary>
        
        public bool Equals(Pair<T1, T2> rhs)
        {
            if (rhs != null)
            {
                if (rhs.Item1.Equals(this.Item1) && rhs.Item2.Equals(this.Item2))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return the hash code, by combining hash codes for the contents.
        /// </summary>
        /// <remarks>Both members must be non-null before this will work; I think that
        /// makes sense.</remarks>
        
        public override int GetHashCode()
        {
            return CombineHashCodes.Combine(this.Item1.GetHashCode(), this.Item2.GetHashCode());
        }

        /// <summary>
        /// Overloaded comparison operator.
        /// </summary>
        
        public static bool operator ==(Pair<T1, T2> lhs, Pair<T1, T2> rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (Object.ReferenceEquals(rhs, null))
                {
                    return false;
                }
                else
                {
                    return lhs.Equals(rhs);
                }
            }
        }

        /// <summary>
        /// Overloaded comparison operator.
        /// </summary>
        
        public static bool operator !=(Pair<T1, T2> lhs, Pair<T1, T2> rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Pairs are stringified by stringifying their contents.
        /// </summary>
        
        public sealed override string ToString()
        {
            StringBuilder combined = new StringBuilder("(");
            if (this.Item1 != null)
            {
                combined.Append(this.Item1.ToString());
            }
            combined.Append(", ");
            if (this.Item2 != null)
            {
                combined.Append(this.Item2.ToString());
            }
            combined.Append(")");
            return combined.ToString();
        }
    }

    /// <summary>
    /// Helper class to enable type inference 
    /// by the compiler.
    /// </summary>
    
    public static class Pair
    {

        /// <summary>
        /// Constructs a pair from its arguments, allowing
        /// the compiler to determine the types.
        /// </summary>
        
        public static Pair<T1,T2> Make<T1,T2>(T1 first, T2 second)
        {
            return new Pair<T1, T2>(first, second);
        }
    }
}
