using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DStarLite 
{
    static Heap U;
    static State[,] S;
    static double km;
    static State sgoal;
    static State sstart;

    // sx and sy are the start coordinates and gx and gy are the goal coordinates
    public static void RunDStarLite(int sx, int sy, int gx, int gy, DStarLiteEnvironment env)
    {
        sstart = new State();
        sstart.x = sx;
        sstart.y = sy;
        sgoal = new State();
        sgoal.x = gx;
        sgoal.y = gy;
        State slast = sstart;
        Initialize();
        ComputeShortestPath();
        while (!sstart.Equals(sgoal))
        {
            // if(sstart.g.isInfinity) then there is no known path
            sstart = MinSuccState(sstart);
            env.MoveTo(new Coordinates(sstart.x, sstart.y));
            LinkedList<Coordinates> obstacleCoord = env.GetObstaclesInVision();
            double oldkm = km;
            State oldslast = slast;
            km += Heuristic(sstart, slast);
            slast = sstart;
            bool change = false;
            foreach (Coordinates c in obstacleCoord)
            {
                State s = S[c.x, c.y];
                if (s.isObstacle) continue;// is already known
                change = true;
                s.isObstacle = true;
                foreach (State p in s.GetPredecessors())
                {
                    UpdateVertex(p);
                }
            }
            if (!change)
            {
                km = oldkm;
                slast = oldslast;
            }
            ComputeShortestPath();
        }
    }

    // calculates the key
    /*
    Priority of a vertex = key
    Key – vector with 2 components
        k(s) = [ k1(s);  k2(s) ]

    k1(s) = min(g(s), rhs(s)) + h(s, sstart) + km
    k2(s) = min(g(s), rhs(s)) 
    */
    static Key CalculateKeyFromStart(State s)
    {
        return new Key(min(s.gCost, s.rightHandSideCost) + Heuristic(s, sstart) + km, min(s.gCost, s.rightHandSideCost));
    }

    static double Heuristic(State a, State b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // runs on a 100*100 plane
    static void Initialize()
    {
        U = new Heap(10000);
        S = new State[100, 100];
        km = 0;
        for (int i = 0; i < S.GetLength(0); i++)
        {
            for (int j = 0; j < S.GetLength(1); j++)
            {
                S[i, j] = new State();
                S[i, j].x = i;
                S[i, j].y = j;
                S[i, j].gCost = Double.PositiveInfinity;
                S[i, j].rightHandSideCost = Double.PositiveInfinity;
            }
        }
        sgoal = S[sgoal.x, sgoal.y];
        sstart = S[sstart.x, sstart.y];
        sgoal.rightHandSideCost = 0;
        U.Insert(sgoal, CalculateKeyFromStart(sgoal));
    }

    static void UpdateVertex(State u)
    {
        if (!u.Equals(sgoal))
        {
            u.rightHandSideCost = MinSucc(u);
        }
        if (U.Contains(u))
        {
            U.Remove(u);
        }
        if (u.gCost != u.rightHandSideCost)
        {
            U.Insert(u, CalculateKeyFromStart(u));
        }
    }

    static State MinSuccState(State u)
    {
        double min = Double.PositiveInfinity;
        State n = null;
        foreach (State s in u.GetSuccessors())
        {
            double val = 1 + s.gCost;
            if (val <= min && !s.isObstacle)
            {
                min = val;
                n = s;
            }
        }
        return n;
    }

    // finds the succesor s' with the min (c(u,s')+g(s'))
    // where cost from u to s' is 1 and returns the value
    static double MinSucc(State u)
    {
        double min = Double.PositiveInfinity;
        foreach (State s in u.GetSuccessors())
        {
            double val = 1 + s.gCost;
            if (val < min && !s.isObstacle) min = val;
        }
        return min;
    }

    
    static void ComputeShortestPath()
    {
        // The while loop continues until the top key in the priority queue is greater than or equal to the key of the start state
        // or until the right-hand-side cost of the start state is equal to its g-cost
        while (U.TopKey().CompareTo(CalculateKeyFromStart(sstart)) < 0 || sstart.rightHandSideCost != sstart.gCost)
        {
            // Store the top key in the priority queue
            Key oldKey = U.TopKey();
            // Pop the state with the smallest key from the priority queue
            State currentState = U.Pop();
            if (currentState == null) break;
            // If the old key is less than the current key of state u
            if (oldKey.CompareTo(CalculateKeyFromStart(currentState)) < 0)
            {
                // Re-insert state u into the priority queue with its new key
                U.Insert(currentState, CalculateKeyFromStart(currentState));
            }
            // If the g-cost of state u is greater than its right-hand-side cost
            else if (currentState.gCost > currentState.rightHandSideCost)
            {
                // Set the g-cost of state u to its right-hand-side cost
                currentState.gCost = currentState.rightHandSideCost;
                // Update all predecessors of state u
                foreach (State s in currentState.GetPredecessors())
                {
                    UpdateVertex(s);
                }
            }
            else
            {
                // Set the g-cost of state u to positive infinity
                currentState.gCost = Double.PositiveInfinity;
                // Update state u and all its predecessors
                UpdateVertex(currentState);
                foreach (State s in currentState.GetPredecessors())
                {
                    UpdateVertex(s);
                }
            }
        }
    }

    static double min(double a, double b)
    {
        if (b < a) return b;
        return a;
    }

    class State
    {
        public int x;
        public int y;
        public double gCost;
        public double rightHandSideCost;
        public bool isObstacle;

        public bool Equals(State that)
        {
            if (this.x == that.x && this.y == that.y) return true;
            return false;
        }

        public LinkedList<State> GetSuccessors()
        {
            LinkedList<State> s = new LinkedList<State>();
            // add succesors in counter clockwise order
            if (x + 1 < S.GetLength(0)) s.AddFirst(S[x + 1, y]);
            if (y + 1 < S.GetLength(1)) s.AddFirst(S[x, y + 1]);
            if (x - 1 >= 0) s.AddFirst(S[x - 1, y]);
            if (y - 1 >= 0) s.AddFirst(S[x, y - 1]);
            return s;
        }

        public LinkedList<State> GetPredecessors()
        {
            LinkedList<State> s = new LinkedList<State>();
            State tempState;
            // add predecessors in counter clockwise order if they are not an obstacle
            if (x + 1 < S.GetLength(0))
            {
                tempState = S[x + 1, y];
                if (!tempState.isObstacle) s.AddFirst(tempState);
            }
            if (y + 1 < S.GetLength(1))
            {
                tempState = S[x, y + 1];
                if (!tempState.isObstacle) s.AddFirst(tempState);
            }
            if (x - 1 >= 0)
            {
                tempState = S[x - 1, y];
                if (!tempState.isObstacle) s.AddFirst(tempState);
            }
            if (y - 1 >= 0)
            {
                tempState = S[x, y - 1];
                if (!tempState.isObstacle) s.AddFirst(tempState);
            }
            return s;
        }
    }

    class Key
    {
        public double k1;
        public double k2;

        public Key(double K1, double K2)
        {
            k1 = K1;
            k2 = K2;
        }

        public int CompareTo(Key that)
        {
            if (this.k1 < that.k1) return -1;
            else if (this.k1 > that.k1) return 1;
            if (this.k2 > that.k2) return 1;
            else if (this.k2 < that.k2) return -1;
            return 0;
        }
    }

    class HeapElement
    {
        public State s;
        public Key Key;

        public HeapElement(State state, Key key)
        {
            s = state;
            Key = key;
        }
    }

    // min heap
    class Heap
    {
        private int n;
        private HeapElement[] heap;
        private Dictionary<State, int> hash;

        public Heap(int cap)
        {
            n = 0;
            heap = new HeapElement[cap];
            hash = new Dictionary<State, int>();
        }

        public Key TopKey()
        {
            if (n == 0) return new Key(Double.PositiveInfinity, Double.PositiveInfinity);
            return heap[1].Key;
        }

        public State Pop()
        {
            if (n == 0) return null;
            State s = heap[1].s;
            heap[1] = heap[n];
            hash[heap[1].s] = 1;
            hash[s] = 0;
            n--;
            moveDown(1);
            return s;
        }

        public void Insert(State s, Key key)
        {
            HeapElement e = new HeapElement(s, key);
            n++;
            hash[s] = n;
            if (n == heap.Length) increaseCap();
            heap[n] = e;
            moveUp(n);
        }

        public void Update(State s, Key key)
        {
            int i = hash[s];
            if (i == 0) return;
            Key kold = heap[i].Key;
            heap[i].Key = key;
            if (kold.CompareTo(key) < 0)
            {
                moveDown(i);
            }
            else
            {
                moveUp(i);
            }
        }

        public void Remove(State s)
        {
            int i = hash[s];
            if (i == 0) return;
            hash[s] = 0;
            heap[i] = heap[n];
            hash[heap[i].s] = i;
            n--;
            moveDown(i);
        }

        public bool Contains(State s)
        {
            int i;
            if (!hash.TryGetValue(s, out i))
            {
                return false;
            }
            return i != 0;
        }

        private void moveDown(int i)
        {
            int childL = i * 2;
            if (childL > n) return;
            int childR = i * 2 + 1;
            int smallerChild;
            if (childR > n)
            {
                smallerChild = childL;
            }
            else if (heap[childL].Key.CompareTo(heap[childR].Key) < 0)
            {
                smallerChild = childL;
            }
            else
            {
                smallerChild = childR;
            }
            if (heap[i].Key.CompareTo(heap[smallerChild].Key) > 0)
            {
                swap(i, smallerChild);
                moveDown(smallerChild);
            }
        }

        private void moveUp(int i)
        {
            if (i == 1) return;
            int parent = i / 2;
            if (heap[parent].Key.CompareTo(heap[i].Key) > 0)
            {
                swap(parent, i);
                moveUp(parent);
            }
        }

        private void swap(int i, int j)
        {
            HeapElement temp = heap[i];
            heap[i] = heap[j];
            hash[heap[j].s] = i;
            heap[j] = temp;
            hash[temp.s] = j;
        }

        private void increaseCap()
        {
            Array.Resize<HeapElement>(ref heap, heap.Length * 2);
        }
    }
}

    public class Coordinates
    {
        public int x;
        public int y;

        public Coordinates(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public interface DStarLiteEnvironment
    {
        void MoveTo(Coordinates s);
        LinkedList<Coordinates> GetObstaclesInVision();
    }


 class DStarLiteTesting : MonoBehaviour , DStarLiteEnvironment
 {
     private void Start()
     {
         DStarLite.RunDStarLite(0, 1, 8, 6, this);
     }

     int time = 0;
     int px = 0;
     int py = 1;

     public void MoveTo(Coordinates s)
     {
         time++;
         px = s.x;
         py = s.y;
         Debug.Log("Move " + time + ": " + px + " " + py);
     }

     public LinkedList<Coordinates> GetObstaclesInVision()
     {
         LinkedList<Coordinates> l = new LinkedList<Coordinates>();
         if (time == 1)
         {
             l.AddFirst(new Coordinates(1, 2));
             l.AddFirst(new Coordinates(1, 3));
             l.AddFirst(new Coordinates(0, 3));
         }

         if (time == 2)
         {
             l.AddFirst(new Coordinates(3, 1));
             l.AddFirst(new Coordinates(3, 2));
         }

         if (py == 3)
         {
             l.AddFirst(new Coordinates(2, 4));
             l.AddFirst(new Coordinates(5, 4));
         }

         if (px == 4)
         {
             l.AddFirst(new Coordinates(5, 1));
             l.AddFirst(new Coordinates(5, 2));
         }

         if (px == 5)
         {
             l.AddFirst(new Coordinates(7, 1));
         }

         if (px == 6)
         {
             l.AddFirst(new Coordinates(8, 2));
         }

         if (px == 7)
         {
             l.AddFirst(new Coordinates(8, 1));
         }

         if (py == 5)
         {
             l.AddFirst(new Coordinates(7, 5));
             l.AddFirst(new Coordinates(5, 6));
             l.AddFirst(new Coordinates(4, 7));
         }

         if (py == 3)
         {
             l.AddFirst(new Coordinates(8, 3));
         }

         if (px == 4 || py == 3)
         {
             l.AddFirst(new Coordinates(4, 4));
         }

         if (py == 3 || px == 6)
         {
             l.AddFirst(new Coordinates(7, 4));
         }

         if (px < 3 && py > 4)
         {
             l.AddFirst(new Coordinates(1, 6));
         }

         if ((px == 3 && py != 0) || py == 5)
         {
             l.AddFirst(new Coordinates(3, 6));
         }

         return l;
     }
 }
