using ICU4N.Support.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.ICU.Support.Text
{
    public sealed class CharacterIteratorWrapper : ICharacterEnumerator
    {
        private CharacterIterator characterIterator;

        public CharacterIteratorWrapper(CharacterIterator characterIterator)
        {
            this.characterIterator = characterIterator ?? throw new ArgumentNullException(nameof(characterIterator));
        }

        public CharacterIterator CharacterIterator => characterIterator;

        public int StartIndex => characterIterator.BeginIndex;

        public int EndIndex => Math.Max(characterIterator.EndIndex - 1, 0);

        public int Length => characterIterator.EndIndex - characterIterator.BeginIndex;

        public int Index
        {
            get
            {
                int index = characterIterator.Index;
                return index >= 0 && index <= EndIndex ? index : CharacterIterator.Done;
            }
            set
            {
                if (value < characterIterator.BeginIndex || value > EndIndex)
                    throw new ArgumentOutOfRangeException(nameof(value), "Invalid index");

                characterIterator.SetIndex(value);
            }
        }

        public char Current => 0 < Index && Index <= EndIndex ? characterIterator.Current : CharacterIterator.Done;

        object IEnumerator.Current => Current;

        public object Clone()
        {
            CharacterIteratorWrapper clone = (CharacterIteratorWrapper)base.MemberwiseClone();
            clone.characterIterator = (CharacterIterator)characterIterator.Clone();
            return clone;
        }

        public void Dispose()
        {
            // Nothing to do
        }

        public bool MoveFirst()
        {
            return characterIterator.First() != CharacterIterator.Done;
        }

        public bool MoveLast()
        {
            return characterIterator.Last() != CharacterIterator.Done;
        }

        public bool MoveNext()
        {
            return characterIterator.Next() != CharacterIterator.Done;
        }

        public bool MovePrevious()
        {
            return characterIterator.Previous() != CharacterIterator.Done;
        }

        public void Reset()
        {
            characterIterator.SetIndex(0);
        }

        public bool TrySetIndex(int value)
        {
            return characterIterator.SetIndex(value) != CharacterIterator.Done;
        }
    }
}
