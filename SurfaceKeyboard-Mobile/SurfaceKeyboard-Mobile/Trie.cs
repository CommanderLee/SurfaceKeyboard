using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceKeyboard_Mobile
{
    class Trie
    {
        public char tag;
        public Trie[] children;
        public int freq;

        public Trie(char _tag)
        {
            tag = _tag;
            //children = new Trie[26];
            freq = 0;
        }

        // assume that child is lowercase
        public void addChild(string child, int childFreq)
        {
            freq += childFreq;

            if (child == "")
            {
                // leaf node
                return;
            }
            else
            {
                // has at least one child
                if (children == null)
                {
                    children = new Trie[26];
                }

                int childNo = child[0] - 'a';

                if (children[childNo] == null)
                {
                    // Empty child
                    children[childNo] = new Trie(child[0]);
                }

                // Update this child
                children[childNo].addChild(child.Substring(1), childFreq);
            }
        }

        public int findChild(string child)
        {
            int ret = 0;
            if (child == "")
            {
                // Complete. Return this value
                ret = freq;
            }
            else
            {
                if (children != null && children[child[0] - 'a'] != null)
                {
                    // Keep searching.
                    ret = children[child[0] - 'a'].findChild(child.Substring(1));
                }
                else
                {
                    // Not found.
                    ret = 0;
                }
            }
            return ret;
        }
    }
}
