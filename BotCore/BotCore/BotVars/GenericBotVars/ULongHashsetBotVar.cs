using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BotCoreNET.BotVars;
using JSON;

namespace BotCoreNET.BotVars.GenericBotVars
{
    public class ULongHashsetBotVar : IGenericBotVar, ISet<ulong>
    {
        private HashSet<ulong> hashset = new HashSet<ulong>();

        #region redirects
        public int Count => ((ISet<ulong>)hashset).Count;

        public bool IsReadOnly => ((ISet<ulong>)hashset).IsReadOnly;

        public bool Add(ulong item)
        {
            return ((ISet<ulong>)hashset).Add(item);
        }

        public void Clear()
        {
            ((ISet<ulong>)hashset).Clear();
        }

        public bool Contains(ulong item)
        {
            return ((ISet<ulong>)hashset).Contains(item);
        }

        public void CopyTo(ulong[] array, int arrayIndex)
        {
            ((ISet<ulong>)hashset).CopyTo(array, arrayIndex);
        }

        public void ExceptWith(IEnumerable<ulong> other)
        {
            ((ISet<ulong>)hashset).ExceptWith(other);
        }

        public IEnumerator<ulong> GetEnumerator()
        {
            return ((ISet<ulong>)hashset).GetEnumerator();
        }

        public void IntersectWith(IEnumerable<ulong> other)
        {
            ((ISet<ulong>)hashset).IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<ulong> other)
        {
            return ((ISet<ulong>)hashset).IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<ulong> other)
        {
            return ((ISet<ulong>)hashset).IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<ulong> other)
        {
            return ((ISet<ulong>)hashset).IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<ulong> other)
        {
            return ((ISet<ulong>)hashset).IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<ulong> other)
        {
            return ((ISet<ulong>)hashset).Overlaps(other);
        }

        public bool Remove(ulong item)
        {
            return ((ISet<ulong>)hashset).Remove(item);
        }

        public bool SetEquals(IEnumerable<ulong> other)
        {
            return ((ISet<ulong>)hashset).SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<ulong> other)
        {
            ((ISet<ulong>)hashset).SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<ulong> other)
        {
            ((ISet<ulong>)hashset).UnionWith(other);
        }

        void ICollection<ulong>.Add(ulong item)
        {
            ((ISet<ulong>)hashset).Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ISet<ulong>)hashset).GetEnumerator();
        }

        #endregion
        public bool ApplyJSON(JSONContainer json)
        {
            if (json.IsArray)
            {
                foreach (JSONField field in json.Array)
                {
                    if (field.IsNumber && !field.IsSigned && !field.IsFloat)
                    {
                        hashset.Add(field.Unsigned_Int64);
                    }
                }
                return true;
            }
            return false;
        }

        public JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewArray();
            foreach (ulong val in hashset)
            {
                result.Add(val);
            }
            return result;
        }
    }
}
