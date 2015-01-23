using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib
{
    //this interface is neccessary to be able to hot-swap a item in the middle of the tree
    public interface IMinHeapIndexable
    {
        int getHeapIndex();
        void setHeapIndex(int index);
    }
    public class MinHeap<T> where T : IComparable<T>, IMinHeapIndexable
    {
        private List<T> items;
        public MinHeap() {
            items = new List<T>();
        }
        public void Add(T item) {
            //Add to bottom of tree and move up
            items.Add(item);
            item.setHeapIndex(items.Count - 1);
            siftUp(items.Count - 1);
        }
        public int Count
        {
            get
            {
                return items.Count;
            }
        }
        public T Peek()
        {
            return items[0];
        }
        public T Pop()
        {
            //Get root node by swapping it with the last element, moving it down again
            T ret = items[0];
            swap(0, items.Count - 1);
            items.RemoveAt(items.Count - 1);
            if(items.Count > 0)
                siftDown(0);
            return ret;
        }
        public void Changed(T item)
        {
            //if a item is changed in the middle of the tree, it needs to be moved up or down
            siftUp(item.getHeapIndex());
            siftDown(item.getHeapIndex());  
        }
        private void swap(int pos1, int pos2)
        {
            T item1 = items[pos1];
            items[pos1] = items[pos2];
            items[pos2] = item1;
            item1.setHeapIndex(pos2);
            items[pos1].setHeapIndex(pos1);
        }
        private void siftUp(int pos)
        {
            if (pos != 0)
            {
                int parentpos = (int)Math.Floor(((double)pos - 1) / 2);
                if (items[parentpos].CompareTo(items[pos]) > 0) //Parent is greater
                {
                    swap(pos, parentpos);
                    siftUp(parentpos);
                }
            }
        }
        private void siftDown(int pos)
        {
            T item = items[pos];
            int leftpos = 2 * pos + 1;
            int rightpos = 2 * pos + 2;
                        
            if (rightpos < items.Count && item.CompareTo(items[rightpos]) > 0 && items[leftpos].CompareTo(items[rightpos]) > 0) 
            {
                //Right exists, is smaller than item, and is smaller than left: go right
                swap(rightpos, pos);
                siftDown(rightpos);
            }
            else if (leftpos < items.Count && item.CompareTo(items[leftpos]) > 0) 
            {
                // Left exists, and is smaller than item: go left
                swap(leftpos, pos);
                siftDown(leftpos);
            }
             
        }
        
    }
}
