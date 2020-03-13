using J2N.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Support.Text
{
    public class CharacterIteratorWrapper : ICharacterEnumerator
    {
        private ICharacterEnumerator enumerator;

        public CharacterIteratorWrapper(ICharacterEnumerator enumerator)
        {

        }

        public int StartIndex => throw new NotImplementedException();

        public int EndIndex => throw new NotImplementedException();

        public int Length => throw new NotImplementedException();

        public int Index { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public char Current => throw new NotImplementedException();

        object IEnumerator.Current => throw new NotImplementedException();

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool MoveFirst()
        {
            throw new NotImplementedException();
        }

        public bool MoveLast()
        {
            throw new NotImplementedException();
        }

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public bool MovePrevious()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public bool TrySetIndex(int value)
        {
            throw new NotImplementedException();
        }
    }
}
