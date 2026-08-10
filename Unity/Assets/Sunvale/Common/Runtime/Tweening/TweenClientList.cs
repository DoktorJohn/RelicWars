using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.Tweening
{
    public class TweenClientList<T> where T:ITweenClient
    {
       private T[] array;
        private int _countPlusOne = 1; 
        private int _expandedCount;
        private int currLen;

        public TweenClientList(int initialLimit)
        {
            if (initialLimit < 2) initialLimit = 2;
            array = new T[initialLimit];
            currLen = initialLimit;
        }

        public void Add(T thing)
        {
            
            if (_countPlusOne >= currLen)
            {
                ExpandArray();
            }
            int idx = _countPlusOne;
            array[idx] = thing;
            thing.SetIndexNumber(idx);
            _countPlusOne = idx + 1;
        }

        public void Remove(T thing)
        {
            int idx = thing.GetIndexNumber();
            if (idx <= 0)
            {
                return;
            }
            int lastIdx = _countPlusOne - 1; // last occupied
            if (idx == lastIdx)
            {
                array[lastIdx] = default;
                _countPlusOne--;
                thing.SetIndexNumber(0);
                return;
            }
            
            T last = array[lastIdx];
            array[idx] = last;
            array[lastIdx] = default;

            last.SetIndexNumber(idx);
            thing.SetIndexNumber(0);
            _countPlusOne--;
        }

        public void RemoveAt(int index)
        {
            if (index <= 0) return;
            T thing = array[index];
            int lastIdx = _countPlusOne - 1;
            if (index == lastIdx)
            {
                array[lastIdx] = default;
                _countPlusOne--;
                thing.SetIndexNumber(0);
                return;
            }

            T last = array[lastIdx];
            array[index] = last;
            array[lastIdx] = default;

            last.SetIndexNumber(index);
            thing.SetIndexNumber(0);
            _countPlusOne--;
        }

        public void UpdateDeltaTime(float deltaTime)
        {
            for (int i = 1; i < _countPlusOne; i++)
            {
                var t = array[i];
                t.CustomUpdate(deltaTime);
            }
        }

        private void ExpandArray()
        {
            int newCapacity = array.Length + (array.Length / 4) + 100;
            Array.Resize(ref array, newCapacity);
            currLen = newCapacity;
            _expandedCount++;

    #if UNITY_EDITOR
            if (_expandedCount > 3)
            {
                Debug.LogWarning($"EXPANDING THIS ARRAY TOO MUCH? Investigate {this} expanded count {_expandedCount}, you might be not unregistering your tweens");
            }
    #endif
        }
        
    }
}
