using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathAI
{
    namespace PathFinding
    {
        // Enumerator state indicator for pathfinder
        public enum PathFinderStatus
        {
            Inert,
            Success,
            Failure,
            Running
        }

        // Create abstract Node<T> class used as base for vertices in pathfinding
        abstract public class Node<T>
        {
            // Store reference to T as auto property
            public T Value { get; private set; }

            // Construct Node
            public Node(T value)
            {
                Value = value;
            }

            // Find neighbor nodes to this node
            abstract public List<Node<T>> GetNeighbors();
        }

        // Abstract generic PathFinder class for core implementation.
        abstract public class PathFinder<T>
        {

            // Create a delegate that accepts two nodes so we can calculate the cost of movement
            public delegate float CostFunction(T a, T b);
            public CostFunction HeuristicCost { get; set; }
            public CostFunction NodeTraversalCost { get; set; }


            // PathFinderNode class is a node in the pathfinder search tree
            public class PathFinderNode
            {
                // Define node parent
                public PathFinderNode Parent { get; set; }

                // Node this PathFinderNode is referring to
                public Node<T> Location { get; private set; }

                // Cost of movement variables
                public float FinalCost { get; private set; }
                public float AccumulatedCost { get; private set; }
                public float HeuristicCost { get; private set; }

                // Construct the PathFinderNode
                public PathFinderNode(Node<T> location, PathFinderNode parent, float accumulatedCost, float heuristicCost)
                {
                    Location = location;
                    Parent = parent;
                    HeuristicCost = heuristicCost;
                    SetAccumulatedCost(accumulatedCost);
                }

                // Method to set the accumulated cost
                public void SetAccumulatedCost(float cost)
                {
                    AccumulatedCost = cost;
                    FinalCost = AccumulatedCost + HeuristicCost;
                }
            }


            // Add property that holds pathfinder status.
            public PathFinderStatus Status { get; private set; } = PathFinderStatus.Inert;

            // Add properties for start and end nodes
            public Node<T> Start { get; private set; }
            public Node<T> End { get; private set; }

            // Property to access the current node
            public PathFinderNode CurrentNode { get; private set; }



            // Open list (unexplored nodes)
            protected List<PathFinderNode> _openList = new List<PathFinderNode>();
            // Closed list (explored nodes)
            protected List<PathFinderNode> _closedList = new List<PathFinderNode>();


            // Create helper method to find least cost node
            protected PathFinderNode GetLeastCostNode(List<PathFinderNode> nodeList)
            {
                int bestIndex = 0;
                float bestPriority = nodeList[0].FinalCost;
                for (int i = 1; i < nodeList.Count; i++)
                {
                    if (bestPriority > nodeList[i].FinalCost)
                    {
                        bestPriority = nodeList[i].FinalCost;
                        bestIndex = i;
                    }
                }

                PathFinderNode node = nodeList[bestIndex];
                return node;
            }

            // Create helper method to check if T is in the list
            // Return index of item where value is found, else return -1

            protected int IsInList(List<PathFinderNode> pfNodeList, T cell)
            {
                for (int i = 0; i < pfNodeList.Count; i++)
                {
                    if (EqualityComparer<T>.Default.Equals(pfNodeList[i].Location.Value, cell))
                    {
                        return i;
                    }
                }
                return -1;
            }

            // Delegates for action callbacks
            public delegate void DelegatePathFinderNode(PathFinderNode node);
            public DelegatePathFinderNode onChangeCurrentNode;
            public DelegatePathFinderNode onAddToOpenList;
            public DelegatePathFinderNode onAddToClosedList;
            public DelegatePathFinderNode onDestinationFound;

            public Action<Node<T>> onCellTraversal;

            public delegate void DelegateNoArgument();
            public DelegateNoArgument onStarted;
            public DelegateNoArgument onRunning;
            public DelegateNoArgument onFailure;
            public DelegateNoArgument onSuccess;

            public bool Initialize(Node<T> start, Node<T> goal)
            {
                if (Status == PathFinderStatus.Running)
                {
                    // Pathfinding is in progress
                    return false;
                }

                // Reset variables
                Reset();

                // Set start and goal nodes
                Start = start;
                End = goal;

                // Calculate the HCost for start
                float hCost = HeuristicCost(start.Value, goal.Value);

                // Create a root node with null parent
                PathFinderNode root = new PathFinderNode(Start, null, 0f, hCost);

                // Add root node to open list
                _openList.Add(root);

                // Set current node to root node
                CurrentNode = root;

                // Invoke delegates
                onChangeCurrentNode?.Invoke(CurrentNode);
                onStarted?.Invoke();

                // Set status to Running
                Status = PathFinderStatus.Running;

                return true;
            }

            public PathFinderStatus Step()
            {
                // Add current node to closed list
                _closedList.Add(CurrentNode);

                // Inform delegate subscribers that a node was added to the closed list
                onAddToClosedList?.Invoke(CurrentNode);

                if (_openList.Count == 0)
                {
                    //no solution found
                    Status = PathFinderStatus.Failure;
                    onFailure?.Invoke();
                    return Status;
                }

                // Get least-cost element from open list and make that the new current node
                //add the least cost node to a list
                //on cell traversal 
                
                CurrentNode = GetLeastCostNode(_openList);
                onCellTraversal?.Invoke(CurrentNode.Location);
                

                // Inform delegate subscribers that the current node has changed
                onChangeCurrentNode?.Invoke(CurrentNode);
                onCellTraversal?.Invoke(CurrentNode.Location);

                //Remove the node from the open list
                _openList.Remove(CurrentNode);

                //Check if current node is end node
                if (EqualityComparer<T>.Default.Equals(CurrentNode.Location.Value, End.Value))
                {
                    Status = PathFinderStatus.Success;
                    // Inform delegate subscribers that destination was found
                    onDestinationFound?.Invoke(CurrentNode);
                    onSuccess?.Invoke();
                    return Status;
                }

                // Find neighboring cells
                List<Node<T>> neighbors = CurrentNode.Location.GetNeighbors();

                // Traverse neighbors
                foreach (Node<T> cell in neighbors)
                {
                    AlgorithmSpecificImplementation(cell);
                }

                Status = PathFinderStatus.Running;
                // Inform delegate subscribers the search is on
                onRunning?.Invoke();
                return Status;
            }

            abstract protected void AlgorithmSpecificImplementation(Node<T> cell);

            // Reset variables for next search;
            protected void Reset()
            {
                if (Status == PathFinderStatus.Running)
                {
                    //cannot reset, pathfinder is running
                    return;
                }

                CurrentNode = null;
                _openList.Clear();
                _closedList.Clear();

                Status = PathFinderStatus.Inert;
            }
        }

        // A* Pathfinder
        public class AStarPathfinder<T> : PathFinder<T>
        {
            protected override void AlgorithmSpecificImplementation(Node<T> cell)
            {
                // Check if cell is already closed
                if (IsInList(_closedList, cell.Value) == -1)
                {
                    // Cell is not closed

                    // Calculate cost of node from its parent
                    float accumulatedCost = CurrentNode.AccumulatedCost + NodeTraversalCost(CurrentNode.Location.Value, cell.Value);
                    float heuristicCost = HeuristicCost(cell.Value, End.Value);

                    // Check if cell is in open list
                    int openListID = IsInList(_openList, cell.Value);
                    if (openListID == -1)
                    {
                        // Add cell to open list
                        PathFinderNode node = new PathFinderNode(cell, CurrentNode, accumulatedCost, heuristicCost);
                        _openList.Add(node);
                        // Inform delegate subscribers that node was added to open list
                        onAddToOpenList?.Invoke(node);
                    }
                    else
                    {
                        // If cell exists in open list, check accumulted cost is less than cost in list
                        float oldAccumulatedCost = _openList[openListID].AccumulatedCost;
                        if (accumulatedCost < oldAccumulatedCost)
                        {
                            // Change parent and update cost to new accumulated cost
                            _openList[openListID].Parent = CurrentNode;
                            _openList[openListID].SetAccumulatedCost(accumulatedCost);
                            // Inform delegate subscribers that a node was added to open list
                            onAddToOpenList?.Invoke(_openList[openListID]);
                        }
                    }
                }
            }
        }
    }
}
