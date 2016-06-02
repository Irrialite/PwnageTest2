﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NetworkBase.Collections
{
    /// <summary>
    /// Copy from .net List<T>. Added faster AddRange methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class FastList<T> : IList<T>, System.Collections.IList, IReadOnlyList<T>
    {
        private const int _defaultCapacity = 4;

        private T[] _items;
        [ContractPublicPropertyName("Count")]
        private int _size;
        private int _version;
        private Object _syncRoot;

        static readonly T[] _emptyArray = new T[0];

        // Constructs a List. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to _defaultCapacity, and then increased in multiples of two
        // as required.
        public FastList()
        {
            _items = _emptyArray;
        }

        // Constructs a List with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        // 
        public FastList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, SR.ArgumentOutOfRange_NeedNonNegNum);
            Contract.EndContractBlock();

            if (capacity == 0)
                _items = _emptyArray;
            else
                _items = new T[capacity];
        }

        // Constructs a List, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        // 
        public FastList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            Contract.EndContractBlock();

            ICollection<T> c = collection as ICollection<T>;
            if (c != null)
            {
                int count = c.Count;
                if (count == 0)
                {
                    _items = _emptyArray;
                }
                else
                {
                    _items = new T[count];
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }
            else
            {
                _size = 0;
                _items = _emptyArray;
                // This enumerable could be empty.  Let Add allocate a new array, if needed.
                // Note it will also go to _defaultCapacity first, not 1, then 2, etc.

                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Add(en.Current);
                    }
                }
            }
        }

        // Gets and sets the capacity of this list.  The capacity is the size of
        // the internal array used to hold items.  When set, the internal 
        // array of the list is reallocated to the given capacity.
        // 
        public int Capacity
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _items.Length;
            }
            set
            {
                if (value < _size)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.ArgumentOutOfRange_SmallCapacity);
                }
                Contract.EndContractBlock();

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        var items = new T[value];
                        Array.Copy(_items, 0, items, 0, _size);
                        _items = items;
                    }
                    else
                    {
                        _items = _emptyArray;
                    }
                }
            }
        }

        // Read-only property describing how many elements are in the List.
        public int Count
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _size;
            }
        }

        public T[] Buffer
        {
            get
            {
                return _items;
            }
        }

        bool System.Collections.IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }


        // Is this List read-only?
        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool System.Collections.IList.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        // Is this List synchronized (thread-safe)?
        bool System.Collections.ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        // Synchronization root for this object.
        Object System.Collections.ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }
        // Sets or Gets the element at the given index.
        // 
        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException();
                }
                Contract.EndContractBlock();
                return _items[index];
            }

            set
            {
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException();
                }
                Contract.EndContractBlock();
                _items[index] = value;
                _version++;
            }
        }

        private static bool IsCompatibleObject(object value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>. 
            return ((value is T) || (value == null && default(T) == null));
        }

        Object System.Collections.IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                if (value == null && !(default(T) == null))
                    throw new ArgumentNullException(nameof(value));

                try
                {
                    this[index] = (T)value;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_WrongType, value, typeof(T)), nameof(value));
                }
            }
        }

        // Adds the given object to the end of this list. The size of the list is
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element.
        //
        public void Add(T item)
        {
            if (_size == _items.Length)
                EnsureCapacity(_size + 1);
            _items[_size++] = item;
            _version++;
        }

        int System.Collections.IList.Add(Object item)
        {
            if (item == null && !(default(T) == null))
                throw new ArgumentNullException(nameof(item));

            try
            {
                Add((T)item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(SR.Format(SR.Arg_WrongType, item, typeof(T)), nameof(item));
            }

            return Count - 1;
        }


        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public void AddRange(IEnumerable<T> collection)
        {
            Contract.Ensures(Count >= Contract.OldValue(Count));

            InsertRange(_size, collection);
        }

        public void AddRange(T[] array)
        {
            AddRange(array, array.Length, 0);
        }

        public void AddRange(T[] array, int count)
        {
            AddRange(array, count, 0);
        }

        public void AddRange(T[] array, int count, int arrayOffset)
        {
            Contract.Ensures(Count >= Contract.OldValue(Count));

            InsertRangeArray(_size, count, arrayOffset, array);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }

        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search.
        // 
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (_size - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.Ensures(Contract.Result<int>() <= index + count);
            Contract.EndContractBlock();

            return Array.BinarySearch<T>(_items, index, count, item, comparer);
        }

        public int BinarySearch(T item)
        {
            Contract.Ensures(Contract.Result<int>() <= Count);
            return BinarySearch(0, Count, item, null);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            Contract.Ensures(Contract.Result<int>() <= Count);
            return BinarySearch(0, Count, item, comparer);
        }


        // Clears the contents of List.
        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
                _size = 0;
            }
            _version++;
        }

        // Contains returns true if the specified element is in the List.
        // It does a linear, O(n) search.  Equality is determined by calling
        // item.Equals().
        //
        public bool Contains(T item)
        {
            if ((Object)item == null)
            {
                for (int i = 0; i < _size; i++)
                    if ((Object)_items[i] == null)
                        return true;
                return false;
            }
            else
            {
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                for (int i = 0; i < _size; i++)
                {
                    if (c.Equals(_items[i], item))
                        return true;
                }
                return false;
            }
        }

        bool System.Collections.IList.Contains(Object item)
        {
            if (IsCompatibleObject(item))
            {
                return Contains((T)item);
            }
            return false;
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
        void System.Collections.ICollection.CopyTo(Array array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }
            Contract.EndContractBlock();

            try
            {
                // Array.Copy will check for NULL.
                Array.Copy(_items, 0, array, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
            }
        }

        // Copies a section of this list to the given array at the given index.
        // 
        // The method uses the Array.Copy method to copy the elements.
        // 
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (_size - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            Contract.EndContractBlock();

            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the current capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.
        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                //if ((uint)newCapacity > Array.MaxArrayLength) newCapacity = Array.MaxArrayLength;
                if (newCapacity < min)
                    newCapacity = min;
                Capacity = newCapacity;
            }
        }

        public bool Exists(Predicate<T> match)
        {
            return FindIndex(match) != -1;
        }

        public T Find(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            Contract.EndContractBlock();

            for (int i = 0; i < _size; i++)
            {
                if (match(_items[i]))
                {
                    return _items[i];
                }
            }
            return default(T);
        }

        public FastList<T> FindAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            Contract.EndContractBlock();

            FastList<T> list = new FastList<T>();
            for (int i = 0; i < _size; i++)
            {
                if (match(_items[i]))
                {
                    list.Add(_items[i]);
                }
            }
            return list;
        }

        public int FindIndex(Predicate<T> match)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            return FindIndex(0, _size, match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < startIndex + Count);
            return FindIndex(startIndex, _size - startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if ((uint)startIndex > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, SR.ArgumentOutOfRange_Index);
            }

            if (count < 0 || startIndex > _size - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_Count);
            }

            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < startIndex + count);
            Contract.EndContractBlock();

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(_items[i]))
                    return i;
            }
            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            Contract.EndContractBlock();

            for (int i = _size - 1; i >= 0; i--)
            {
                if (match(_items[i]))
                {
                    return _items[i];
                }
            }
            return default(T);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            return FindLastIndex(_size - 1, _size, match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() <= startIndex);
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() <= startIndex);
            Contract.EndContractBlock();

            if (_size == 0)
            {
                // Special case for 0 length List
                if (startIndex != -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, SR.ArgumentOutOfRange_Index);
                }
            }
            else if ((uint)startIndex >= (uint)_size) // Make sure we're not out of range
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, SR.ArgumentOutOfRange_Index);
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_Count);
            }

            int endIndex = startIndex - count;
            for (int i = startIndex; i > endIndex; i--)
            {
                if (match(_items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            int version = _version;

            for (int i = 0; i < _size; i++)
            {
                if (version != _version)
                {
                    break;
                }
                action(_items[i]);
            }

            if (version != _version)
                throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        //
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public FastList<T> GetRange(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            Contract.Ensures(Contract.Result<FastList<T>>() != null);
            Contract.EndContractBlock();

            FastList<T> list = new FastList<T>(count);
            Array.Copy(_items, index, list._items, 0, count);
            list._size = count;
            return list;
        }


        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            return Array.IndexOf(_items, item, 0, _size);
        }

        int System.Collections.IList.IndexOf(Object item)
        {
            if (IsCompatibleObject(item))
            {
                return IndexOf((T)item);
            }
            return -1;
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and ending at count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item, int index)
        {
            if (index > _size)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return Array.IndexOf(_items, item, index, _size - index);
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and upto count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item, int index, int count)
        {
            if (index > _size)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);

            if (count < 0 || index > _size - count)
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_Count);
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();

            return Array.IndexOf(_items, item, index, count);
        }

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        // 
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_BiggerThanCollection);
            }
            Contract.EndContractBlock();
            if (_size == _items.Length)
                EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
            _version++;
        }

        void System.Collections.IList.Insert(int index, Object item)
        {
            if (item == null && !(default(T) == null))
                throw new ArgumentNullException(nameof(item));

            try
            {
                Insert(index, (T)item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(SR.Format(SR.Arg_WrongType, item, typeof(T)), nameof(item));
            }
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the List's size.
        //
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            }
            Contract.EndContractBlock();

            ICollection<T> c = collection as ICollection<T>;
            if (c != null)
            {    // if collection is ICollection<T>
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_size + count);
                    if (index < _size)
                    {
                        Array.Copy(_items, index, _items, index + count, _size - index);
                    }

                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (this == c)
                    {
                        // Copy first part of _items to insert location
                        Array.Copy(_items, 0, _items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(_items, index + count, _items, index * 2, _size - index);
                    }
                    else
                    {
                        T[] itemsToInsert = new T[count];
                        c.CopyTo(itemsToInsert, 0);
                        Array.Copy(itemsToInsert, 0, _items, index, count);
                    }
                    _size += count;
                }
            }
            else
            {
                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Insert(index++, en.Current);
                    }
                }
            }
            _version++;
        }

        public void InsertRangeArray(int index, int count, int arrayOffset, T[] array)
        {
            int arrayLen = array.Length;

            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            }

            if ((uint)arrayOffset >= (uint)arrayLen)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayOffset), arrayOffset, SR.ArgumentOutOfRange_Index);
            }

            if ((uint)count > (uint)arrayLen)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_Index);
            }

            if (arrayLen - arrayOffset < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            if (count > 0)
            {
                EnsureCapacity(_size + count);
                if (index < _size)
                {
                    Array.Copy(_items, index, _items, index + count, _size - index);
                }

                Array.Copy(array, arrayOffset, _items, index, count);
                _size += count;
            }
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end 
        // and ending at the first element in the list. The elements of the list 
        // are compared to the given value using the Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            if (_size == 0)
            {  // Special case for empty list
                return -1;
            }
            else
            {
                return LastIndexOf(item, _size - 1, _size);
            }
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and ending at the first element in the list. The 
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index)
        {
            if (index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(((Count == 0) && (Contract.Result<int>() == -1)) || ((Count > 0) && (Contract.Result<int>() <= index)));
            Contract.EndContractBlock();
            return LastIndexOf(item, index, index + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and upto count elements. The elements of
        // the list are compared to the given value using the Object.Equals
        // method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index, int count)
        {
            if ((Count != 0) && (index < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if ((Count != 0) && (count < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(((Count == 0) && (Contract.Result<int>() == -1)) || ((Count > 0) && (Contract.Result<int>() <= index)));
            Contract.EndContractBlock();

            if (_size == 0)
            {  // Special case for empty list
                return -1;
            }

            if (index >= _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            }

            if (count > index + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_Count);
            }

            return Array.LastIndexOf(_items, item, index, count);
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        void System.Collections.IList.Remove(Object item)
        {
            if (IsCompatibleObject(item))
            {
                Remove((T)item);
            }
        }

        // This method removes all items which matches the predicate.
        // The complexity is O(n).   
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= Contract.OldValue(Count));
            Contract.EndContractBlock();

            int freeIndex = 0;   // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < _size && !match(_items[freeIndex]))
                freeIndex++;
            if (freeIndex >= _size)
                return 0;

            int current = freeIndex + 1;
            while (current < _size)
            {
                // Find the first item which needs to be kept.
                while (current < _size && match(_items[current]))
                    current++;

                if (current < _size)
                {
                    // copy item to the free slot.
                    _items[freeIndex++] = _items[current++];
                }
            }

            Array.Clear(_items, freeIndex, _size - freeIndex);
            int result = _size - freeIndex;
            _size = freeIndex;
            _version++;
            return result;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            }
            Contract.EndContractBlock();
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T);
            _version++;
        }

        // Removes a range of elements from this list.
        // 
        public void RemoveRange(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            if (count > 0)
            {
                int i = _size;
                _size -= count;
                if (index < _size)
                {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                Array.Clear(_items, _size, count);
                _version++;
            }
        }

        // Reverses the elements in this list.
        public void Reverse()
        {
            Reverse(0, Count);
        }

        // Reverses the elements in a range of this list. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        // 
        public void Reverse(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            // The non-generic Array.Reverse is not used because it does not perform
            // well for non-primitive value types.
            // If/when a generic Array.Reverse<T> becomes available, the below code
            // can be deleted and replaced with a call to Array.Reverse<T>.
            int i = index;
            int j = index + count - 1;
            T[] array = _items;
            while (i < j)
            {
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
                i++;
                j--;
            }
            _version++;
        }

        // Sorts the elements in this list.  Uses the default comparer and 
        // Array.Sort.
        public void Sort()
        {
            Sort(0, Count, null);
        }

        // Sorts the elements in this list.  Uses Array.Sort with the
        // provided comparer.
        public void Sort(IComparer<T> comparer)
        {
            Sort(0, Count, comparer);
        }

        // Sorts the elements in a section of this list. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented by all
        // elements of the list.
        // 
        // This method uses the Array.Sort method to sort the elements.
        // 
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            Array.Sort<T>(_items, index, count, comparer);
            _version++;
        }

        public void Sort(Comparison<T> comparison)
        {
            if (comparison == null)
            {
                throw new ArgumentNullException(nameof(comparison));
            }
            Contract.EndContractBlock();

            if (_size > 0)
            {
                IComparer<T> comparer = Comparer<T>.Create(comparison);
                Array.Sort(_items, 0, _size, comparer);
            }
        }

        // ToArray returns a new Object array containing the contents of the List.
        // This requires copying the List, which is an O(n) operation.
        public T[] ToArray()
        {
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == Count);

            if (_size == 0)
            {
                return _emptyArray;
            }

            T[] array = new T[_size];
            Array.Copy(_items, 0, array, 0, _size);
            return array;
        }

        // Sets the capacity of this list to the size of the list. This method can
        // be used to minimize a list's memory overhead once it is known that no
        // new elements will be added to the list. To completely clear a list and
        // release all memory referenced by the list, execute the following
        // statements:
        // 
        // list.Clear();
        // list.TrimExcess();
        // 
        public void TrimExcess()
        {
            int threshold = (int)(((double)_items.Length) * 0.9);
            if (_size < threshold)
            {
                Capacity = _size;
            }
        }

        public bool TrueForAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            Contract.EndContractBlock();

            for (int i = 0; i < _size; i++)
            {
                if (!match(_items[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private FastList<T> list;
            private int index;
            private int version;
            private T current;

            internal Enumerator(FastList<T> list)
            {
                this.list = list;
                index = 0;
                version = list._version;
                current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                FastList<T> localList = list;

                if (version == localList._version && ((uint)index < (uint)localList._size))
                {
                    current = localList._items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (version != list._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                index = list._size + 1;
                current = default(T);
                return false;
            }

            public T Current
            {
                get
                {
                    return current;
                }
            }

            Object System.Collections.IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == list._size + 1)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (version != list._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                index = 0;
                current = default(T);
            }
        }


        /// <summary>
        /// Copy from .net.
        /// </summary>
        private static class SR
        {
            public const string Arg_WrongType = "The value '{0}' is not of type '{1}' and cannot be used in this generic collection.";
            public const string InvalidOperation_EnumFailedVersion = "Collection was modified; enumeration operation may not execute.";
            public const string InvalidOperation_EnumOpCantHappen = "Enumeration has either not started or has already finished.";
            public const string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";
            public const string Argument_InvalidOffLen = "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.";
            public const string Argument_InvalidArrayType = "Target array type is not compatible with the type of items in the collection.";
            public const string ArgumentOutOfRange_Index = "Index was out of range. Must be non-negative and less than the size of the collection.";
            public const string ArgumentOutOfRange_Count = "Count must be positive and count must refer to a location within the string/array/collection.";
            public const string ArgumentOutOfRange_BiggerThanCollection = "Must be less than or equal to the size of the collection.";
            public const string Arg_RankMultiDimNotSupported = "Only single dimensional arrays are supported for the requested action.";
            public const string ArgumentOutOfRange_SmallCapacity = "capacity was less than the current size.";

            internal static string Format(string resourceFormat, params object[] args)
            {
                if (args != null)
                {
                    return string.Format(resourceFormat, args);
                }

                return resourceFormat;
            }


            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static string Format(string resourceFormat, object p1)
            {
                return string.Format(resourceFormat, p1);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static string Format(string resourceFormat, object p1, object p2)
            {
                return string.Format(resourceFormat, p1, p2);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static string Format(string resourceFormat, object p1, object p2, object p3)
            {
                return string.Format(resourceFormat, p1, p2, p3);
            }

        }
    }
}