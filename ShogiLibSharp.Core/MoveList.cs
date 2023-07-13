//using System;
//namespace ShogiLibSharp.Core
//{
//    public class MoveList : IReadOnlyList<Move>
//    {
//        internal Move[] buffer;
//        internal int size;

//        public Move this[int index] => buffer[index];
//        public int Count => size;

//        internal MoveList()
//        {
//            this.buffer = new Move[600];
//            this.size = 0;
//        }

//        internal MoveList(Move[] buffer, int size)
//        {
//            this.buffer = buffer;
//            this.size = size;
//        }

//        internal void Add(Move m)
//        {
//            buffer[size++] = m;
//        }

//        public Enumerator GetEnumerator()
//        {
//            return new Enumerator(buffer, size);
//        }

//        IEnumerator<Move> IEnumerable<Move>.GetEnumerator()
//        {
//            return GetEnumerator();
//        }

//        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
//        {
//            throw new NotImplementedException();
//        }

//        public struct Enumerator : IEnumerator<Move>
//        {
//            readonly Move[] moves;
//            readonly int size;
//            int currentIndex = -1;

//            internal Enumerator(Move[] moves, int size)
//            {
//                this.moves = moves;
//                this.size = size;
//            }

//            public Move Current => moves[currentIndex];

//            object System.Collections.IEnumerator.Current => Current;

//            public void Dispose()
//            {
//            }

//            public bool MoveNext()
//            {
//                return ++currentIndex < size;
//            }

//            public void Reset()
//            {
//                currentIndex = -1;
//            }
//        }
//    }
//}

