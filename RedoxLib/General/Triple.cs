using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxLib.General
{
    /// <summary>
    /// A simple class containing two objects.  Pairs can be compared for equality and stored in
    /// dictionaries.
    /// </summary>
    /// <remarks>An equivalent C++ type would also support cross-casting to pairs of related types,
    /// but I don't know how to do that in C#.</remarks>
    
    public class Triple<T1, T2, T3>
    {
        /// <summary>
        /// The first item in the pair.
        /// </summary>
        
        public T1 Item1;

        /// <summary>
        /// The second item in the pair.
        /// </summary>
        
        public T2 Item2;

        public T3 Item3;

        /// <summary>
        /// Constructs a pair initialised with the given items.
        /// </summary>
        
        public Triple(T1 item1, T2 item2, T3 item3)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        
        public Triple(Triple<T1, T2, T3> rhs)
        {
            this.Item1 = rhs.Item1;
            this.Item2 = rhs.Item2;
            this.Item3 = rhs.Item3;
        }

        /// <summary>
        /// Construct an empty pair.
        /// </summary>

        public Triple()
        {
        }

        /// <summary>
        /// Test equality.
        /// </summary>
        
        public override bool Equals(object obj)
        {
            Triple<T1, T2, T3> rhs = obj as Triple<T1, T2, T3>;

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

        public bool Equals(Triple<T1, T2, T3> rhs)
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
            return CombineHashCodes.Combine(this.Item1.GetHashCode(), this.Item2.GetHashCode(), this.Item3.GetHashCode());
        }

        /// <summary>
        /// Overloaded comparison operator.
        /// </summary>

        public static bool operator ==(Triple<T1, T2, T3> lhs, Triple<T1, T2, T3> rhs)
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

        public static bool operator !=(Triple<T1, T2, T3> lhs, Triple<T1, T2, T3> rhs)
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
            combined.Append(", ");
            if (this.Item3 != null)
            {
                combined.Append(this.Item3.ToString());
            }
            combined.Append(")");
            return combined.ToString();
        }
    }

    /// <summary>
    /// Helper class to enable type inference 
    /// by the compiler.
    /// </summary>
    
    public static class Triple
    {

        /// <summary>
        /// Constructs a pair from its arguments, allowing
        /// the compiler to determine the types.
        /// </summary>
        
        public static Triple<T1,T2, T3> Make<T1,T2,T3>(T1 first, T2 second, T3 third)
        {
            return new Triple<T1, T2, T3>(first, second, third);
        }

        public static Triple<T1, T2, T3> Make<T1, T2, T3>(Pair<T1, T2> pair, T3 third)
        {
            return new Triple<T1, T2, T3>(pair.Item1, pair.Item2, third);
        }
    }
}
