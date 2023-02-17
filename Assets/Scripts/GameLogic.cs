using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLogic : MonoBehaviour
{
    private enum Direction { UP, RIGHT, DOWN, LEFT }
    public enum GameState
    {
        SPAWN, // spawning candy
        WAIT, // wait for candy moving to its position
        CHECK, // check matching candy
        READY, // ready for player to control
    }

    private static int maxCol = 15;
    private static int maxRow = 8;
    private static float dragThreshold = 0.2f;
    private float spawnDelayTime = 0.2f;
    private static System.Random rnd = new System.Random();
    private static GameObject[,] candies = new GameObject[maxRow, maxCol];
    private static int[] spawnQueue = new int[maxCol];

    public GameState gameState;
    private GameObject selectedObject;
    private Vector2 startMousePosition;
    private GameObject lastMoveObject;
    private float spawnTimer;
    private GameObject lastMovedObject;
    private Direction lastInvertMovedDirection;
    private bool skipCheck = false;
    private int score = 0;

    public int minMatch = 3;
    public int scorePerCandy = 100;
    public Text scoreText;
    public GameObject[] prototypes;

    // Start is called before the first frame update
    void Start()
    {
        gameState = GameState.SPAWN;
        for (int i = 0; i < spawnQueue.Length; i++)
        {
            spawnQueue[i] = maxRow;
        }
        spawnTimer = spawnDelayTime; // want first wave candy to spawn immedialy
    }

    // Update is called once per frame
    void Update()
    {
        switch (gameState)
        {
            case GameState.SPAWN:
                spawnTimer += Time.deltaTime;
                if (spawnTimer > spawnDelayTime)
                {
                    bool isEmpty = spawnByQueue();
                    if (isEmpty)
                    {
                        spawnTimer = spawnDelayTime;
                        gameState = GameState.WAIT;
                    }
                    else
                    {
                        spawnTimer = 0f;
                    }
                }
                break;
            case GameState.WAIT:
                Candy lastSpawnCandy = lastMoveObject.GetComponent("Candy") as Candy;
                if (lastSpawnCandy.ready) {
                    if (skipCheck)
                    {
                        skipCheck = false;
                        gameState = GameState.READY;
                    }
                    else gameState = GameState.CHECK;
                }
                break;
            case GameState.CHECK:
                bool hasMatched = checkAll();
                if (hasMatched)
                {
                    lastMovedObject = null;
                    moveDownCandy();
                    gameState = GameState.SPAWN;
                }
                else if (lastMovedObject != null)
                {
                    moveCandy(lastMovedObject, lastInvertMovedDirection);
                    lastMovedObject = null;
                    skipCheck = true;
                }
                else
                {
                    gameState = GameState.READY;
                }
                break;
            case GameState.READY:
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Collider2D targetObject = Physics2D.OverlapPoint(mousePosition);
                    if (!targetObject) return;

                    selectedObject = targetObject.transform.gameObject;
                    startMousePosition = mousePosition;
                    Debug.Log("select " + (selectedObject.GetComponent("Candy") as Candy));

                }
                else if (selectedObject && Input.GetMouseButtonUp(0))
                {
                    Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    float deltaX = Math.Abs(startMousePosition.x - mousePosition.x);
                    float deltaY = Math.Abs(startMousePosition.y - mousePosition.y);

                    if (deltaX > deltaY)
                    {
                        if (deltaX >= dragThreshold)
                        {
                            Direction moveDirection = startMousePosition.x - mousePosition.x > 0 ? Direction.LEFT : Direction.RIGHT;
                            lastMovedObject = selectedObject.gameObject;
                            lastInvertMovedDirection = moveDirection == Direction.RIGHT ? Direction.LEFT : Direction.RIGHT; ;
                            moveCandy(selectedObject.gameObject, moveDirection);
                        }
                    }
                    else
                    {
                        if (deltaY >= dragThreshold)
                        {
                            Direction moveDirection = startMousePosition.y - mousePosition.y > 0 ? Direction.DOWN : Direction.UP;
                            lastMovedObject = selectedObject.gameObject;
                            lastInvertMovedDirection = moveDirection == Direction.UP ? Direction.DOWN : Direction.UP;
                            moveCandy(selectedObject.gameObject, moveDirection);
                        }
                    }

                    selectedObject = null;
                }
                break;
        }
    }

    bool spawnByQueue()
    {
        bool queueEmpty = true;
        for (int j = 0; j < spawnQueue.Length; j++)
        {
            if (spawnQueue[j] > 1) queueEmpty &= false;
            if (spawnQueue[j] == 0) continue;

            int i = maxRow - spawnQueue[j];
            spawnCandy(i, j);
            spawnQueue[j]--;
        }
        return queueEmpty;
    }

    void spawnCandy(int row, int col)
    {
        int randomIndex = rnd.Next(0, prototypes.Length);
        GameObject prototype = prototypes[randomIndex];
        GameObject obj = Instantiate(prototype);
        candies[row, col] = obj;
        lastMoveObject = obj;

        Candy candy = obj.GetComponent("Candy") as Candy;
        candy.colorId = randomIndex;
        candy.row = row;
        candy.col = col;

        //Debug.Log("spawn" + candy);
    }

    void moveDownCandy()
    {
        for (int j = 0; j < maxCol; j++)
        {
            if (spawnQueue[j] == 0) continue;
            int emptyRowIndex = 0;
            for (int i = 0; i < maxRow; i++)
            {
                if (candies[i, j] == null) continue;
                setCandyDestination(candies[i, j], emptyRowIndex++, j);
            }
        }

        gameState = GameState.WAIT;
    }

    void moveCandy(GameObject obj, Direction direction)
    {
        Candy candy = obj.GetComponent("Candy") as Candy;
        //Debug.Log("move " + (selectedObject.GetComponent("Candy") as Candy) + " " + direction);
        int i = candy.row;
        int j = candy.col;
        switch (direction)
        {
            case Direction.UP: i += 1; break;
            case Direction.DOWN: i -= 1; break;
            case Direction.RIGHT: j += 1; break;
            case Direction.LEFT: j -= 1; break;
        }

        GameObject pairObj;
        try
        {
            pairObj = candies[i, j];
        }
        catch (IndexOutOfRangeException)
        {
            return;
        }

        setCandyDestination(pairObj, candy.row, candy.col);
        setCandyDestination(obj, i, j);

        lastMoveObject = obj;
        gameState = GameState.WAIT;
    }
    void setCandyDestination(GameObject candyObj, int row, int col)
    {
        candies[row, col] = candyObj;
        Candy candy = candyObj.GetComponent("Candy") as Candy;
        candy.MoveTo(row, col);
    }

    bool checkAll()
    {
        HashSet<GameObject> matchObjs = new HashSet<GameObject>();
        List<GameObject> mayMatchObjs = new List<GameObject>();

        int lastColorId;
        int count;

        // check horizontal
        for (int i = 0; i < maxRow; i++)
        {
            lastColorId = -1;
            count = 0;
            for (int j = 0; j < maxCol; j++)
            {
                Candy candy = candies[i, j].GetComponent("Candy") as Candy;
                if (lastColorId != candy.colorId)
                {
                    if (count >= minMatch)
                    { // matched move all mayMatch to match
                        matchObjs.UnionWith(mayMatchObjs);
                    }
                    lastColorId = candy.colorId;
                    count = 1;
                    mayMatchObjs.Clear();
                }
                else
                {
                    count++;
                }
                mayMatchObjs.Add(candies[i, j]);
            }
            if (count >= minMatch)
            { // for last candy in row
                matchObjs.UnionWith(mayMatchObjs);
            }
        }

        // check vertical
        for (int j = 0; j < maxCol; j++)
        {
            lastColorId = -1;
            count = 0;
            for (int i = 0; i < maxRow; i++)
            {
                Candy candy = candies[i, j].GetComponent("Candy") as Candy;
                if (lastColorId != candy.colorId)
                {
                    if (count >= minMatch)
                    { // matched move all mayMatch to match
                        matchObjs.UnionWith(mayMatchObjs);
                    }
                    lastColorId = candy.colorId;
                    count = 1;
                    mayMatchObjs.Clear();
                }
                else
                {
                    count++;
                }
                mayMatchObjs.Add(candies[i, j]);
            }

            if (count >= minMatch)
            { // for last candy in col
                matchObjs.UnionWith(mayMatchObjs);
            }
        }

        score += matchObjs.Count * scorePerCandy;
        if (scoreText != null) scoreText.text = "Score: " + score.ToString();

        destroyCandies(matchObjs);
        return matchObjs.Count > 0;
    }

    void destroyCandies(IEnumerable<GameObject> objs)
    {
        foreach (GameObject obj in objs)
        {
            Candy matchedCandy = obj.GetComponent("Candy") as Candy;
            spawnQueue[matchedCandy.col]++;
            candies[matchedCandy.row, matchedCandy.col] = null;
            Destroy(obj);
        }
    }
}
