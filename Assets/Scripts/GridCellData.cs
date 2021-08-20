using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathAI.PathFinding;

public class GridCellData : Node<Vector2Int>
{
    // Enum to set status of cell
    public enum CellStatus
    {
        Open,
        Wall,
        Start,
        End
    }
    
    public CellStatus CurrentStatus { get; set; }

    //// Property to check if cell is walkable
    //public bool IsWalkable { get; set; }
    //public bool IsStart { get; set; }
    //public bool IsEnd { get; set; }

    // Maybe? Declare reference to grid cell visualizer
    // private GridCellVisualizer _gridCellVisualizer;

    // Declare reference to grid to find neighbors
    private GridVisualizer _gridVisualizer;

    // Construct the node with grid and location
    public GridCellData(GridVisualizer gridVisualizer, Vector2Int value) : base(value)
    {
        _gridVisualizer = gridVisualizer;

        // Cells are open by default
        CurrentStatus = CellStatus.Open;
    }

    // Get neighbors
    public override List<Node<Vector2Int>> GetNeighbors()
    {
        return _gridVisualizer.GetNeighborCells(this);
    }
}
