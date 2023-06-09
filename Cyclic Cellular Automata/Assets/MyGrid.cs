using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyGrid : MonoBehaviour
{



    public Vector2Int size;
    public Vector2Int wantedSize;
    public Vector2Int currentSize;
    public Cell[,] cells;
    public float cellSize = 1;
    public GameObject cellPrefab;

    public bool gridVisible = false;

    private Vector2Int currentStep;  // Track the current step in the grid generation


    public int maxState = 2;
    public int minState = 0;
    public int range = 1;
    public int threshold = 2;
    public int neighborhoodCount;
    public int skipStableTestIterations = 5;

    public int neighborhood = 0;
    public bool warp = false;
    public Toggle warpToggle;
    public bool stableState = false;
    public bool play = false;
    public float playSpeed = 0.5f;
    public Button playButton;
    public Camera cam;

    public Color[] colorArray;

    public Vector2Int testCellPos = Vector2Int.zero;

    public ColorsPanel colorsPanel;

    [Header("Sliders")]

    public Slider gridSizeSlider;
    public TMP_Text gridSizeText;

    public Slider cellSizeSlider;
    public TMP_Text cellSizeText;

    public Slider speedSlider;
    public TMP_Text speedText;

    public Slider statesSlider;
    public TMP_Text statesText;

    public Slider thresholdSlider;
    public TMP_Text thresholdText;

    public Slider rangeSlider;
    public TMP_Text rangeText;

    public TMP_Dropdown neighborhoodDropdown;

    public Button generateGridButton;

    [Header("Random")]

    public bool changeState;

    [Range(1, 8)]
    public int minRandState = 1;

    [Range(1, 8)]
    public int maxRandState = 8;

    public bool changeThreshold;

    [Range(1, 20)]
    public int minThreshold = 1;

    [Range(1, 30)]
    public int maxThreshold = 20;
    public int thresholdLimit = 30;
    public bool changeRange;

    [Range(1, 20)]
    public int minRange = 1;

    [Range(1, 20)]
    public int maxRange = 20;
    public int rangeLimit = 20;
    public bool changeNeighborhood;
    public bool changeWarp;
    public bool changeColor;
    public bool skipStable;

    [Header("Experimental")]

    public Vector2Int maxSize;

    public TMP_InputField gridSizeInputField;

    private void Start()
    {
        GenerateCellsOneTime();
        cam = Camera.main;
        //cells = new Cell[size.x, size.y];
        neighborhoodCount = CalculateNeighborCount(range);

        gridSizeSlider.onValueChanged.AddListener(SetGridSizeFromSlider);
        cellSizeSlider.onValueChanged.AddListener(SetCellSizeFromSlider);
        speedSlider.onValueChanged.AddListener(SetSpeedFromSlider);
        statesSlider.onValueChanged.AddListener(SetStatesFromSlider);
        thresholdSlider.onValueChanged.AddListener(SetThresholdFromSlider);
        rangeSlider.onValueChanged.AddListener(SetRangeFromSlider);
        neighborhoodDropdown.onValueChanged.AddListener(SetNeighborhoodFromDropdown);
        UpdateUIElements();

    }

    public void UpdateUIElements()
    {
        gridSizeSlider.value = size.x;
        gridSizeText.text = (int)size.x + "x" + (int)size.y;

        wantedSize = size;
        statesSlider.value = maxState + 1;
        thresholdSlider.maxValue = thresholdLimit;
        thresholdSlider.value = threshold;
        rangeSlider.maxValue = rangeLimit;
        rangeSlider.value = range;
        neighborhoodDropdown.value = neighborhood;
        warpToggle.isOn = warp;




    }
    public void RandomizeSettings()
    {
        if (changeState)
        {
            maxState = Random.Range(minRandState - 1, maxRandState);
            statesSlider.value = maxState + 1;
            ResetCells();
        }

        if (changeThreshold)
        {
            threshold = Random.Range(minThreshold, maxThreshold + 1);
            thresholdSlider.value = threshold;
        }

        if (changeRange)
        {
            range = Random.Range(minRange, maxRange + 1);
            rangeSlider.value = range;
        }

        if (changeNeighborhood)
        {
            neighborhood = Random.Range(0, neighborhoodDropdown.options.Count); // Assumes 10 available neighborhood types
            neighborhoodDropdown.value = neighborhood;
        }

        if (changeWarp)
        {
            warp = Random.value < 0.5f; // Randomly sets warp to true or false
            warpToggle.isOn = warp;
        }
        if (changeColor)
        {
            colorsPanel.RandomPalette();
        }
        ResetCells();
        if (skipStable)
        {
            StartCoroutine(SkipStableGrids());
        }
    }

    public void OnGridSizeSliderMove()
    {
        generateGridButton.interactable = true;
    }

    IEnumerator SkipStableGrids()
    {

        for (int i = 0; i < skipStableTestIterations; i++)
        {
            Iterate();

        }
        if (stableState && play)
        {
            RandomizeSettings();
        }
        yield return null;
    }

    IEnumerator IteratorTimer()
    {
        Iterate();

        yield return new WaitForSeconds(playSpeed);

        if (play)
            StartCoroutine(IteratorTimer());

    }
    public void Play()
    {
        if (!gridVisible)
            return;

        play = true;
        playButton.interactable = !play;
        StartCoroutine(IteratorTimer());
    }
    public void Pause()
    {
        if (!gridVisible)
            return;
        play = false;
        playButton.interactable = !play;

    }
    public void Iterate()
    {
        if (!gridVisible)
            return;
        foreach (Cell cell in cells)
        {
            if (HandleThreshold(neighborhood, cell))
            {
                if (cell.state == maxState)
                {
                    cell.nextState = minState;
                }
                else
                {
                    cell.IterateNextState();
                }
            }
            else
            {
                cell.nextState = cell.state;
            }
        }
        UpdateCells();
        stableState = IsGridStable();


    }
    bool HandleThreshold(int neighborhood, Cell cell)
    {
        switch (neighborhood)
        {
            case 0:
                return CheckNeighborCells(GetNeighborCellsMoore(cell), cell.state) >= threshold;
            case 1:
                return CheckNeighborCells(GetNeighborCellsCross(cell), cell.state) >= threshold;
            case 2:
                return CheckNeighborCells(GetNeighborCellsCustom(cell), cell.state) >= threshold;
            case 3:
                return CheckNeighborCells(GetNeighborCellsRemoteMoore(cell), cell.state) >= threshold;
            case 4:
                return CheckNeighborCells(GetNeighborCellsVonNeumann(cell), cell.state) >= threshold;
            case 5:
                return CheckNeighborCells(GetRemoteNeighborCellsVonNeumann(cell), cell.state) >= threshold;
            case 6:
                return CheckNeighborCells(GetNeighborCellsS(cell), cell.state) >= threshold;
            case 7:
                return CheckNeighborCells(GetNeighborCellsBlade(cell), cell.state) >= threshold;
            case 8:
                return CheckNeighborCells(GetNeighborCellsCorners(cell), cell.state) >= threshold;
            case 9:
                return CheckNeighborCells(GetNeighborCellsTickMark(cell), cell.state) >= threshold;
            case 10:
                return CheckNeighborCells(GetNeighborCellsLines(cell), cell.state) >= threshold;
            default:
                return false;
        }
    }

    void UpdateCells()
    {
        foreach (Cell cell in cells)
        {
            cell.UpdateCell();
        }
    }

    public int GetCellState(int x, int y)
    {
        return cells[x, y].state;
    }

    public bool IsGridStable()
    {
        // Check if all cells have the same state as their neighbors in the previous iteration
        for (int i = 0; i < cells.GetLength(0); i++)
        {
            for (int j = 0; j < cells.GetLength(1); j++)
            {

                Cell cell = cells[i, j];
                int currentState = cell.state;
                int previousState = cell.previousState;

                // Compare the current state with the previous state of the cell
                if (currentState != previousState)
                {
                    //Debug.Log();

                    // Grid is not stable, at least one cell has changed
                    return false;
                }
            }
        }

        // Grid is stable, all cells have the same state as their neighbors in the previous iteration
        return true;
    }

    public int CalculateNeighborCount(int range)
    {
        int count = 0;
        for (int i = 1; i <= range; i++)
        {
            count += i * 8;
        }
        return count;
    }

    public Cell[] GetNeighborCellsTickMark(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();

        // Blade Neighborhood
        for (int i = 1; i <= range; i++)
        {
            AddNeighborCell(cellsList, cell.x + i, cell.y + i);
            AddNeighborCell(cellsList, cell.x - i, cell.y - i);
            AddNeighborCell(cellsList, cell.x + i, cell.y - i);
            AddNeighborCell(cellsList, cell.x - i, cell.y + i);
        }

        // Remote Moore Neighborhood
        for (int i = -range; i <= range; i++)
        {
            for (int j = -range; j <= range; j++)
            {
                if (Mathf.Abs(i) == range || Mathf.Abs(j) == range)
                {
                    AddNeighborCell(cellsList, cell.x + i, cell.y + j);
                }
            }
        }

        return cellsList.ToArray();
    }

    public Cell[] GetNeighborCellsCorners(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();

        // Check the +x, +y direction
        for (int i = 1; i <= range; i++)
        {
            for (int j = i; j >= 1; j--)
            {
                AddNeighborCell(cellsList, cell.x + j, cell.y + i);
            }
        }

        // Check the -x, -y direction
        for (int i = 1; i <= range; i++)
        {
            for (int j = i; j >= 1; j--)
            {
                AddNeighborCell(cellsList, cell.x - j, cell.y - i);
            }
        }

        // Check the +x, -y direction
        for (int i = 1; i <= range; i++)
        {
            for (int j = i; j >= 1; j--)
            {
                AddNeighborCell(cellsList, cell.x + j, cell.y - i);
            }
        }

        // Check the -x, +y direction
        for (int i = 1; i <= range; i++)
        {
            for (int j = i; j >= 1; j--)
            {
                AddNeighborCell(cellsList, cell.x - j, cell.y + i);
            }
        }

        return cellsList.ToArray();
    }

    public Cell[] GetNeighborCellsBlade(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();

        // Check the diagonal directions
        for (int i = 1; i <= range; i++)
        {
            // Upper-right diagonal
            AddNeighborCell(cellsList, cell.x + i, cell.y + i);

            // Upper-left diagonal
            AddNeighborCell(cellsList, cell.x - i, cell.y + i);

            // Lower-right diagonal
            AddNeighborCell(cellsList, cell.x + i, cell.y - i);

            // Lower-left diagonal
            AddNeighborCell(cellsList, cell.x - i, cell.y - i);
        }

        return cellsList.ToArray();
    }

    public Cell[] GetNeighborCellsS(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();

        // Iterate over the cells in the positive quadrant
        for (int i = 1; i <= range; i++)
        {
            // +x, +y direction
            AddNeighborCell(cellsList, cell.x + i, cell.y + i);

            // -x, +y direction
            AddNeighborCell(cellsList, cell.x - i, cell.y + i);
        }

        // Iterate over the cells in the negative quadrant
        for (int i = -1; i >= -range; i--)
        {
            // +x, -y direction
            AddNeighborCell(cellsList, cell.x + i, cell.y + i);

            // -x, -y direction
            AddNeighborCell(cellsList, cell.x - i, cell.y + i);
        }

        return cellsList.ToArray();
    }

    public Cell[] GetNeighborCellsCustom(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();

        // Custom neighborhood: Random positions within the range (-7,7) to (7,-7)
        int minOffset = -range;
        int maxOffset = range;

        for (int i = 0; i < range; i++)
        {
            int offsetX = Random.Range(minOffset, maxOffset + 1);
            int offsetY = Random.Range(minOffset, maxOffset + 1);

            // Apply wrapping if enabled
            if (warp)
            {
                offsetX = (cell.x + offsetX + size.x) % size.x;
                offsetY = (cell.y + offsetY + size.y) % size.y;
            }

            // Check if neighbor cell is valid and add it to the list
            if (IsValidCellIndex(offsetX, offsetY))
                cellsList.Add(cells[offsetX, offsetY]);
        }

        return cellsList.ToArray();
    }

    private bool IsValidCellIndex(int x, int y)
    {
        return x >= 0 && x < size.x && y >= 0 && y < size.y;
    }

    public Cell[] GetRemoteNeighborCellsVonNeumann(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();

        // Check north neighbor
        if (cell.y + range < size.y)
            cellsList.Add(cells[cell.x, cell.y + range]);
        else if (warp)
            cellsList.Add(cells[cell.x, cell.y + range - size.y]);

        // Check south neighbor
        if (cell.y - range >= 0)
            cellsList.Add(cells[cell.x, cell.y - range]);
        else if (warp)
            cellsList.Add(cells[cell.x, size.y + cell.y - range]);

        // Check east neighbor
        if (cell.x + range < size.x)
            cellsList.Add(cells[cell.x + range, cell.y]);
        else if (warp)
            cellsList.Add(cells[cell.x + range - size.x, cell.y]);

        // Check west neighbor
        if (cell.x - range >= 0)
            cellsList.Add(cells[cell.x - range, cell.y]);
        else if (warp)
            cellsList.Add(cells[size.x + cell.x - range, cell.y]);

        for (int i = 1; i <= range; i++)
        {
            // +x, +y direction
            AddNeighborCell(cellsList, cell.x + i, cell.y + i);

            // -x, +y direction
            AddNeighborCell(cellsList, cell.x - i, cell.y + i);

            // +x, -y direction
            AddNeighborCell(cellsList, cell.x + i, cell.y - i);

            // -x, -y direction
            AddNeighborCell(cellsList, cell.x - i, cell.y - i);
        }

        return cellsList.ToArray();
    }

    public Cell[] GetNeighborCellsVonNeumann(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();

        // Check north neighbor
        for (int i = 1; i <= range; i++)
        {
            if (cell.y + i < size.y)
                cellsList.Add(cells[cell.x, cell.y + i]);
            else if (warp)
                cellsList.Add(cells[cell.x, cell.y + i - size.y]);
        }

        // Check south neighbor
        for (int i = 1; i <= range; i++)
        {
            if (cell.y - i >= 0)
                cellsList.Add(cells[cell.x, cell.y - i]);
            else if (warp)
                cellsList.Add(cells[cell.x, size.y + cell.y - i]);
        }

        // Check east neighbor
        for (int i = 1; i <= range; i++)
        {
            if (cell.x + i < size.x)
                cellsList.Add(cells[cell.x + i, cell.y]);
            else if (warp)
                cellsList.Add(cells[cell.x + i - size.x, cell.y]);
        }

        // Check west neighbor
        for (int i = 1; i <= range; i++)
        {
            if (cell.x - i >= 0)
                cellsList.Add(cells[cell.x - i, cell.y]);
            else if (warp)
                cellsList.Add(cells[size.x + cell.x - i, cell.y]);
        }
        for (int i = 1; i <= range; i++)
        {
            // +x, +y direction
            AddNeighborCell(cellsList, cell.x + i, cell.y + i);

            // -x, +y direction
            AddNeighborCell(cellsList, cell.x - i, cell.y + i);

            // +x, -y direction
            AddNeighborCell(cellsList, cell.x + i, cell.y - i);

            // -x, -y direction
            AddNeighborCell(cellsList, cell.x - i, cell.y - i);
        }

        return cellsList.ToArray();
    }

    private void AddNeighborCell(List<Cell> cellsList, int x, int y)
    {
        if (warp)
        {
            x = WrapGridIndex(x, size.x);
            y = WrapGridIndex(y, size.y);
        }
        else if (x >= size.x || y >= size.y || x < 0 || y < 0)
        {
            return;
        }

        cellsList.Add(cells[x, y]);
    }

    private int WrapGridIndex(int index, int gridSize)
    {
        return (index % gridSize + gridSize) % gridSize;
    }

    public Cell[] GetNeighborCellsMoore(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();
        //Moore neighborhood: Regular
        //Von neighborhood: Cross-shaped

        for (int i = -range; i <= range; i++)
        {
            for (int j = -range; j <= range; j++)
            {
                if (i == 0 && j == 0)
                    continue;  // Skip the input cell itself

                int neighborX = cell.x + i;
                int neighborY = cell.y + j;
                if (warp)
                {
                    //warp
                    // Wrap around the grid if neighborX or neighborY is outside the grid bounds
                    if (neighborX >= size.x)
                        neighborX -= size.x;
                    else if (neighborX < 0)
                        neighborX += size.x;

                    if (neighborY >= size.y)
                        neighborY -= size.y;
                    else if (neighborY < 0)
                        neighborY += size.y;
                }
                else
                {
                    // Skip the neighbor cell if it's outside the grid bounds
                    if (neighborX >= size.x || neighborY >= size.y || neighborX < 0 || neighborY < 0)
                        continue;
                }


                cellsList.Add(cells[neighborX, neighborY]);
            }
        }
        return cellsList.ToArray();
    }

    public Cell[] GetNeighborCellsRemoteMoore(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();

        for (int i = -range; i <= range; i++)
        {
            for (int j = -range; j <= range; j++)
            {
                if (Mathf.Abs(i) == range || Mathf.Abs(j) == range)
                {
                    AddNeighborCell(cellsList, cell.x + i, cell.y + j);
                }
            }
        }
        return cellsList.ToArray();
    }

    public Cell[] GetNeighborCellsCross(Cell cell)
    {
        List<Cell> cellsList = new List<Cell>();

        // Check north neighbor
        for (int i = 1; i <= range; i++)
        {
            if (cell.y + i < size.y)
                cellsList.Add(cells[cell.x, cell.y + i]);
            else if (warp)
                cellsList.Add(cells[cell.x, cell.y + i - size.y]);
        }

        // Check south neighbor
        for (int i = 1; i <= range; i++)
        {
            if (cell.y - i >= 0)
                cellsList.Add(cells[cell.x, cell.y - i]);
            else if (warp)
                cellsList.Add(cells[cell.x, size.y + cell.y - i]);
        }

        // Check east neighbor
        for (int i = 1; i <= range; i++)
        {
            if (cell.x + i < size.x)
                cellsList.Add(cells[cell.x + i, cell.y]);
            else if (warp)
                cellsList.Add(cells[cell.x + i - size.x, cell.y]);
        }

        // Check west neighbor
        for (int i = 1; i <= range; i++)
        {
            if (cell.x - i >= 0)
                cellsList.Add(cells[cell.x - i, cell.y]);
            else if (warp)
                cellsList.Add(cells[size.x + cell.x - i, cell.y]);
        }

        return cellsList.ToArray();
    }
    public Cell[] GetNeighborCellsLines(Cell cell)
    {


        List<Cell> cellsList = new List<Cell>();

        int x = cell.x;
        int y = cell.y;
        List<int> radii = new List<int>(); // List of random radii values

        for (int i = 0; i < range; i++)
        {
            int randomRadius = Random.Range(0, range + 1);
            radii.Add(randomRadius);
        }


        for (int i = 0; i < radii.Count; i++)
        {
            int radius = radii[i];
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    AddNeighborCell(cellsList, x + dx, y + dy);
                }
            }
        }

        return cellsList.ToArray();


    }

    public int CheckNeighborCells(Cell[] c, int current)
    {
        int count = 0;
        //Debug.Log(current+"< >"+c.Length) ;
        foreach (Cell cell in c)
        {
            if (cell.state == current + 1)
            {
                count++;
            }
            else if (cell.state == minState && current == maxState)
            {
                count++;
            }

        }

        return count;
    }

    public void GenerateCellsOneTime()
    {
        //Debug.Log("gen 1 time");
        cells = new Cell[maxSize.x, maxSize.y];
        LoadColorArray(colorsPanel.GetColorArray());

        Vector2 pos;
        int randomNumber;
        GameObject cell;
        Cell cellScript;
        for (int i = 0; i < maxSize.x; i++)
        {
            for (int j = 0; j < maxSize.y; j++)
            {

                pos = new Vector2(i, j);

                cell = Instantiate(cellPrefab, pos, Quaternion.identity);
                cellScript = cell.GetComponent<Cell>();


                randomNumber = Random.Range(0, maxState + 1);
                //Debug.Log(randomNumber);
                cellScript.SetPosition(i, j);
                cellScript.SetCellScale(cellSize);
                cellScript.SetColorPalette(colorArray);
                cellScript.SetState(randomNumber);

                cells[i, j] = cellScript;


            }
        }
    }
    public void ResizeGrid()
    {
        //Debug.Log("resizing");
        int x= (int)gridSizeSlider.value;
        int y= (int)gridSizeSlider.value;
        int randomNumber;
        size = new Vector2Int(x, y);
        for (int i = 0; i < maxSize.x-1; i++)
        {
            for (int j = 0; j < maxSize.y-1; j++)
            {


                if(i< x-1  && j< y-1)
                {
                    randomNumber = Random.Range(0, maxState + 1);
                    //Debug.Log("ij:" + i+" "+j);

                    cells[i, j].SetCellScale(cellSize);
                    cells[i, j].SetState(randomNumber);
                }
                else
                {
                    //Debug.Log(cells.GetLength(0)+":l i:"+i+" j:"+j);
                    //if(i<cells.GetLength(0)&&j<cells.GetLength(1))
                    
                    cells[i, j].gameObject.GetComponent<SpriteRenderer>().color=Color.black;
                }



            }
        }
    }
 
    public void ResetCells()
    {
        if (currentSize != size)
        {
            GenerateCells();
            return;
        }

        LoadColorArray(colorsPanel.GetColorArray());

        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                Vector2 pos = new Vector2(i, j);
                int randomNumber = Random.Range(0, maxState + 1);

                cells[i, j].SetColorPalette(colorArray);
                cells[i,j].SetCellScale(cellSize);

                cells[i, j].SetState(randomNumber);
                //Debug.Log(randomNumber);


            }
        }
    }
    public void GenerateCells()
    {
        generateGridButton.interactable = false;
        Debug.Log("Generated cells");
        stableState = false;
        size = wantedSize;
        currentSize = size;
        gridVisible = true;
        LoadColorArray(colorsPanel.GetColorArray());
        //KillChildren();
        //HideCells();
        KillCells();
        if (cells.Length < size.x || cells.Length > size.y)
        {
            //Debug.Log(size.x + " " + cells.GetLength(0));
            cells = new Cell[size.x, size.y];

        }
        else
        {

        }
        neighborhoodCount = CalculateNeighborCount(range);
        Vector2 pos;
        int randomNumber;
        GameObject cell;
        Cell cellScript;
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {

                pos = new Vector2(i, j);
                if (cells[i, j] != null)
                {
                    //Debug.Log(pos + ": already exists");
                }
                cell = Instantiate(cellPrefab, pos, Quaternion.identity);
                cellScript = cell.GetComponent<Cell>();


                randomNumber = Random.Range(0, maxState + 1);
                //Debug.Log(randomNumber);
                cellScript.SetPosition(i, j);
                cellScript.SetCellScale(cellSize);
                cellScript.SetColorPalette(colorArray);
                cellScript.SetState(randomNumber);

                cells[i, j] = cellScript;

            }
        }
        UpdateCamera();


    }

    public float sectionSize = 10;
    private List<Coroutine> activeSectionCoroutines = new List<Coroutine>();

    //experimental
    private IEnumerator GenerateGridParallel()
    {

        yield return new WaitForSeconds(0.1f);
        // Activate the loading image

        int sectionsX = Mathf.CeilToInt(size.x / sectionSize);
        int sectionsY = Mathf.CeilToInt(size.y / sectionSize);

        for (int sectionX = 0; sectionX < sectionsX; sectionX++)
        {
            for (int sectionY = 0; sectionY < sectionsY; sectionY++)
            {

                StartCoroutine(GenerateGridSection(sectionX, sectionY));
            }
        }

        // Wait for all sections to finish generating
        while (activeSectionCoroutines.Count > 0)
        {
            yield return null;
        }

        // Grid generation complete
        UpdateCamera();

    }
    private IEnumerator GenerateGridSection(int sectionX, int sectionY)
    {
        Vector2 pos;
        GameObject cell;
        Cell cellScript;
        int randomNumber;
        for (int x = (int)(sectionX * sectionSize); x < (sectionX + 1) * sectionSize && x < size.x; x++)
        {
            for (int y = (int)(sectionY * sectionSize); y < (sectionY + 1) * sectionSize && y < size.y; y++)
            {
                // Generate cell at position (x, y)
                pos = new Vector2(x, y);
                cell = Instantiate(cellPrefab, pos, Quaternion.identity);
                //cell.transform.SetParent(transform);
                cellScript = cell.GetComponent<Cell>();

                randomNumber = Random.Range(0, maxState + 1);

                cellScript.SetPosition(x, y);
                cellScript.SetCellScale(cellSize);
                cellScript.SetColorPalette(colorArray);
                //Debug.Log(randomNumber);
                cellScript.SetState(randomNumber);

                cells[x, y] = cellScript;
            }
        }

        // Remove the coroutine from the active list
        Coroutine coroutineToRemove = activeSectionCoroutines.FirstOrDefault(c => c.ToString() == sectionX + "-" + sectionY);
        if (coroutineToRemove != null)
        {
            activeSectionCoroutines.Remove(coroutineToRemove);
        }

        yield return null;
    }


    // Generate the grid with a coroutine, 
    public void GenerateCellsCoroutine()
    {
        size = wantedSize;
        currentSize = size;
        gridVisible = true;
        LoadColorArray(colorsPanel.GetColorArray());
        KillChildren();
        cells = new Cell[size.x, size.y];
        neighborhoodCount = CalculateNeighborCount(range);

        currentStep = Vector2Int.zero;  // Start from the first step (0, 0)

        // Start the coroutine for step-wise generation
        //StartCoroutine(GenerateGridStepByStep());
        StartCoroutine(GenerateGridParallel());

    }
    private IEnumerator GenerateGridStepByStep()
    {
        UpdateCamera();


        while (currentStep.y < size.y)
        {
            // Generate cells row by row
            GenerateGridStep();

            // Increment the step position
            currentStep.x++;
            if (currentStep.x >= size.x)
            {
                currentStep.x = 0;
                currentStep.y++;
            }

            // Yield to the main thread and allow other processes to execute
            yield return null;
        }

        // Grid generation complete

    }

    private void GenerateGridStep()
    {
        Vector2Int pos = currentStep;

        GameObject cell = Instantiate(cellPrefab, (Vector2)pos, Quaternion.identity);
        //cell.transform.SetParent(transform);
        Cell cellScript = cell.GetComponent<Cell>();

        int randomNumber = Random.Range(0, maxState + 1);

        cellScript.SetPosition(pos.x, pos.y);
        cellScript.SetCellScale(cellSize);
        cellScript.SetColorPalette(colorArray);
        cellScript.SetState(randomNumber);
        //Debug.Log(randomNumber);

        cells[pos.x, pos.y] = cellScript;
    }
   


    public void SetGridSizeFromSlider(float gridSize)
    {
        wantedSize = new Vector2Int((int)gridSize, (int)gridSize);
        gridSizeText.text = (int)gridSize + "x" + (int)gridSize;
    }
    public void SetGridSizeFromInputField()
    {
        if(gridSizeInputField.text == "")
        {

            return;
        }
        int inputValue = int.Parse(gridSizeInputField.text);


        //Debug.Log(1 + " " + inputValue);

        if (inputValue > gridSizeSlider.maxValue)
        {
            inputValue =(int) gridSizeSlider.maxValue;
        }else if (inputValue < gridSizeSlider.minValue )
        {
            inputValue =(int) gridSizeSlider.minValue;
        }
        //Debug.Log(2 + " " + inputValue);

        wantedSize = new Vector2Int(inputValue, inputValue);
        gridSizeText.text = (int)inputValue + "x" + (int)inputValue;
        gridSizeSlider.value = inputValue;
        gridSizeInputField.text = "";

    }
    public void SetCellSizeFromSlider(float cs)
    {
        cellSize = cs;
        cellSizeText.text = (Mathf.Round(cellSize * 100)) / 100.0 + "";

    }
    public void SetSpeedFromSlider(float speed)
    {
        playSpeed = speed;
        speedText.text = "Speed: " + (int)(1 / speed) + " FPS";

    }
    public void SetStatesFromSlider(float s)
    {
        maxState = (int)s - 1;
        statesText.text = "States: " + s;

    }
    public void SetThresholdFromSlider(float t)
    {
        threshold = (int)t;
        thresholdText.text = "Threshold: " + t;
    }
    public void SetRangeFromSlider(float r)
    {
        range = (int)r;
        rangeText.text = "Range: " + r;

    }
    public void SetNeighborhoodFromDropdown(int r)
    {
        neighborhood = (int)r;

    }
    public void ToggleWarpToggle()
    {
        warp = !warp;
    }
    public void HideCells()
    {
        foreach (var cell in cells)
        {
            if (cell != null)
                cell.gameObject.GetComponent<SpriteRenderer>().color = Color.black;
        }
    }
    public void KillChildren()
    {

        int childCount = transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            Destroy(child);
        }
    }
    public void KillCells()
    {
        foreach (var cell in cells)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }

    }
    public void LoadColorArray(Color[] array)
    {
        colorArray = array;
    }

    void UpdateCameraPosition()
    {
        float xOffset = size.x * 0.4f;

        cam.orthographicSize = size.x / 2 + 5;
        cam.transform.position = new Vector2(size.x / 2 + xOffset, size.y / 2);

    }
    public void UpdateCamera()
    {


        float cameraSize = Mathf.Max(size.x, size.y) * 0.5f + 1;
        Vector3 cameraPosition = new Vector3(size.x * 0.5f, size.x * 0.5f, -10f);

        cam.orthographicSize = cameraSize;
        cam.transform.position = cameraPosition;
        cam.transform.GetComponent<ObjectTween>().CalculateCamPosition(size);
        cam.transform.GetComponent<ObjectTween>().TeleportToHiddenPosition();

    }


}
