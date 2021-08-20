using PathAI.PathFinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    // Declare serialized integer variables for max columns and rows
    [SerializeField]
    private int _maxColumns;
    [SerializeField]
    private int _maxRows;

    // Declare serialized object reference variable for grid cell prefab
    [SerializeField]
    private GameObject _gridCellPrefab;
    // Declare a 2D array variable for grid cells
    private GameObject[,] _gridCellGameObjects;
    // Declare a 2D array to store cell indices
    protected Vector2Int[,] _indices;
    // 2D array of the GridCell
    protected GridCellData[,] _gridCellsData;
    // Cell colors
    [SerializeField]
    Color _openColor;
    [SerializeField]
    Color _wallColor;
    [SerializeField]
    Color _startColor;
    [SerializeField]
    Color _pathColor;
    [SerializeField]
    Color _visitedColor;
    [SerializeField]
    Color _endColor;

    // Declare variables for start and end cells
    private GridCellVisualizer _startingCell;
    private GridCellVisualizer _endingCell;

    // Declare a Pathfinder
    private PathFinder<Vector2Int> _pathFinder = new AStarPathfinder<Vector2Int>();

    // Declare an object reference variable for the camera
    private Camera _camera;

    private void Start()
    {
        CacheReferenceVariables();
        _pathFinder.onSuccess = OnSuccessPathFinding;
        _pathFinder.onFailure = OnFailurePathFinding;
        _pathFinder.HeuristicCost = GetEuclideanCost;
        _pathFinder.NodeTraversalCost = GetEuclideanCost;


        ConstructGrid(_maxColumns, _maxRows);

        ResetCamera();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ClickCell();
        }
        if (Input.GetMouseButtonDown(1))
        {
            RightClickCell();
        }
    }

    private void ClickCell()
    {
        Vector2 rayPos = new Vector2(_camera.ScreenToWorldPoint(Input.mousePosition).x, _camera.ScreenToWorldPoint(Input.mousePosition).y);
        RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero, 0f);

        if (hit)
        {
            GridCellVisualizer selectedCell = hit.transform.GetComponent<GridCellVisualizer>();
            if (selectedCell != null)
            {
                ToggleWalkable(selectedCell);
            }
        }
    }

    private void RightClickCell()
    {
        Vector2 rayPos = new Vector2(_camera.ScreenToWorldPoint(Input.mousePosition).x, _camera.ScreenToWorldPoint(Input.mousePosition).y);
        RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero, 0f);

        if (hit)
        {
            GridCellVisualizer selectedCell = hit.transform.GetComponent<GridCellVisualizer>();
            if (selectedCell != null)
            {
                StartAndEndPoint(selectedCell);
            }
        }
    }

    

    private void ToggleWalkable(GridCellVisualizer selectedCell)
    {
        switch (selectedCell.gridCellData.CurrentStatus)
        {
            case GridCellData.CellStatus.Open:
                selectedCell.gridCellData.CurrentStatus = GridCellData.CellStatus.Wall;
                selectedCell.SetInteriorColor(_wallColor);
                break;
            case GridCellData.CellStatus.Wall:
                selectedCell.gridCellData.CurrentStatus = GridCellData.CellStatus.Open;
                selectedCell.SetInteriorColor(_openColor);
                break;
            case GridCellData.CellStatus.Start:
                Debug.Log("Can't put a wall on the start point.  Move the start and try again.");
                break;
            case GridCellData.CellStatus.End:
                Debug.Log("Can't put a wall on the end point.  Move the end and try again.");
                break;
            default:
                break;
        }
    }

    private void StartAndEndPoint(GridCellVisualizer selectedCell)
    {
        if (_pathFinder.Status == PathFinderStatus.Running)
        {
            Debug.Log("Pathfinder already running. Cannot set destination now");
            return;
        }
        switch (selectedCell.gridCellData.CurrentStatus)
        {
            case GridCellData.CellStatus.Open:
                if (_startingCell != null)
                {
                    if (_endingCell != null)
                    {
                        _endingCell.gridCellData.CurrentStatus = GridCellData.CellStatus.Open;
                        _endingCell.SetInteriorColor(_openColor);
                    }
                    _endingCell = selectedCell;
                    selectedCell.gridCellData.CurrentStatus = GridCellData.CellStatus.End;
                    selectedCell.SetInteriorColor(_endColor);
                }
                else
                {
                    _startingCell = selectedCell;
                    selectedCell.gridCellData.CurrentStatus = GridCellData.CellStatus.Start;
                    selectedCell.SetInteriorColor(_startColor);
                }
                break;
            case GridCellData.CellStatus.Wall:
                Debug.Log("Cannot start or end in a wall, try again");
                break;
            case GridCellData.CellStatus.Start:
                _startingCell = null;
                selectedCell.gridCellData.CurrentStatus = GridCellData.CellStatus.Open;
                selectedCell.SetInteriorColor(_openColor);
                break;
            case GridCellData.CellStatus.End:
                _endingCell = null;
                selectedCell.gridCellData.CurrentStatus = GridCellData.CellStatus.Open;
                selectedCell.SetInteriorColor(_openColor);
                break;
            default:
                break;
        }
    }

    private void OnSuccessPathFinding()
    {
        Debug.Log("Valid path solution found!");
    }

    private void OnFailurePathFinding()
    {
        Debug.Log("Cannot find a valid path");
    }

    private void CacheReferenceVariables()
    {
        //assign reference and null check
        _camera = Camera.main;
        if (_camera == null)
        {
            Debug.LogError("Main Camera is null!");
        }
    }

    protected void ConstructGrid(int maxColumns, int maxRows)
    {
        _maxColumns = maxColumns;
        _maxRows = maxRows;

        // Create new matrices based on max columns and rows
        _indices = new Vector2Int[_maxColumns, _maxRows];
        _gridCellGameObjects = new GameObject[_maxColumns, _maxRows];
        _gridCellsData = new GridCellData[_maxColumns, _maxRows];

        // Create grid cell index data and instantiate grid cell game objects
        for (int x = 0; x < _maxColumns; x++)
        {
            for (int y = 0; y < _maxRows; y++)
            {
                _indices[x, y] = new Vector2Int(x, y);
                _gridCellGameObjects[x, y] = Instantiate(_gridCellPrefab, new Vector3(x, y, 0.0f), Quaternion.identity);

                // Set the parent for this grid cell to this
                _gridCellGameObjects[x, y].transform.SetParent(this.transform);
                // Name the cell for easy reference in hierarchy
                _gridCellGameObjects[x, y].name = "Cell_" + x + "_" + y;

                // Create GridCells
                _gridCellsData[x, y] = new GridCellData(this, _indices[x, y]);

                // Reference to grid cell
                GridCellVisualizer gridCell = _gridCellGameObjects[x, y].GetComponent<GridCellVisualizer>();
                if (gridCell != null)
                {
                    gridCell.gridCellData = _gridCellsData[x, y];
                }
            }
        }
    }

    private void ResetCamera()
    {
        // Adjust camera position and size to show the whole grid based on max columns and rows
        _camera.orthographicSize = _maxRows / 2.0f + 1.0f;
        _camera.transform.position = new Vector3(_maxColumns / 2.0f - 0.5f, _maxRows / 2.0f - 0.5f, -100.0f);
    }

    public List<Node<Vector2Int>> GetNeighborCells(Node<Vector2Int> location)
    {
        List<Node<Vector2Int>> neighbors = new List<Node<Vector2Int>>();

        int x = location.Value.x;
        int y = location.Value.y;

        // Check north
        if (y < _maxRows - 1)
        {
            int i = x;
            int j = y + 1;

            if (_gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.Open || _gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.End)
            {
                //change the neighbor's color
                neighbors.Add(_gridCellsData[i, j]);
            }
        }

        // Check northeast
        if ((y < _maxRows - 1) && (x < _maxColumns -1))
        {
            int i = x + 1;
            int j = y + 1;

            if (_gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.Open || _gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.End)
            {
                neighbors.Add(_gridCellsData[i, j]);
            }
        }

        // Check east
        if (x < _maxColumns - 1)
        {
            int i = x + 1;
            int j = y;

            if (_gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.Open || _gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.End)
            {
                neighbors.Add(_gridCellsData[i, j]);
            }
        }

        //Check southeast
        if ((x < _maxColumns -1) && (y > 0))
        {
            int i = x + 1;
            int j = y - 1;

            if (_gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.Open || _gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.End)
            {
                neighbors.Add(_gridCellsData[i, j]);
            }
        }

        // Check south
        if (y > 0)
        {
            int i = x;
            int j = y - 1;

            if (_gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.Open || _gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.End)
            {
                neighbors.Add(_gridCellsData[i, j]);
            }
        }

        // Check southwest
        if ((y > 0) && (x > 0))
        {
            int i = x - 1;
            int j = y - 1;

            if (_gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.Open || _gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.End)
            {
                neighbors.Add(_gridCellsData[i, j]);
            }
        }

        // Check west
        if (x > 0)    
        {
            int i = x - 1;
            int j = y;

            Vector2Int v = _indices[i, j];

            if (_gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.Open || _gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.End)
            {
                neighbors.Add(_gridCellsData[i, j]);
            }
        }

        // Check northwest
        if ((x > 0) && (y < _maxRows -1))
        {
            int i = x - 1;
            int j = y + 1;

            if (_gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.Open || _gridCellsData[i, j].CurrentStatus == GridCellData.CellStatus.End)
            {
                neighbors.Add(_gridCellsData[i, j]);
            }
        }

        return neighbors;
    }

    public static float GetManhattanCost(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public static float GetEuclideanCost(Vector2Int a, Vector2Int b)
    {
        return GetCostBetweenTwoCells(a, b);
    }

    public static float GetCostBetweenTwoCells(Vector2Int a, Vector2Int b)
    {
        return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }

    public GridCellData GetGridCellData(int x, int y)
    {
        if ((x >= 0) && (x < _maxColumns) && (y >= 0) && (y < _maxRows))
        {
            return _gridCellsData[x, y];
        }
        return null;
    }
}
