using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Candy : MonoBehaviour
{
    private static float startX = -8.1f;
    private static float startY = -4.3f;
    private static float dropY = 6f;
    private static float offset = 1.15f;
    private static float velocityX = 6f;
    private static float velocityY = 6f;

    private Vector2 destination;

    public int colorId;
    public int row;
    public int col;
    public bool ready;

    // Start is called before the first frame update
    void Start()
    {
        this.destination = getDestination(row, col);
        gameObject.name = ToString();
        transform.position = new Vector3(this.destination.x, dropY);
    }

	private void Update()
	{
        Vector3 currentPos = transform.position;
        float newX = currentPos.x;
        float newY = currentPos.y;

        ready = currentPos.x == this.destination.x && currentPos.y == this.destination.y;
        if (!ready)
        {
            if (currentPos.x != this.destination.x)
            {
                if (currentPos.x < this.destination.x)
                {
                    newX += velocityX * Time.deltaTime;
                    if (newX > this.destination.x) newX = this.destination.x;
                }
                else
                {
                    newX -= velocityX * Time.deltaTime;
                    if (newX < this.destination.x) newX = this.destination.x;
                }
            }
            else if (currentPos.y != this.destination.y)
            {
                if (currentPos.y != this.destination.y)
                {
                    newY -= velocityY * Time.deltaTime;
                    if (newY < this.destination.y) newY = this.destination.y;
                }
            }
            transform.position = new Vector3(newX, newY);
        }
	}

	private void OnDestroy()
	{
        Debug.Log("destroy " + this);
	}

	public void MoveTo(int row, int col)
    {
        Debug.Log("move " + this + " to " + getA1Notation(row, col));
        this.row = row;
        this.col = col;
        gameObject.name = ToString();
        this.destination = getDestination(row, col);
    }

    private static Vector2 getDestination(int row, int col)
    {
        return new Vector2(startX + col * offset, startY + row * offset);
    }

    public override string ToString()
    {
        return getA1Notation(this.row, this.col);
    }

    public static string getA1Notation(int row, int col)
    {
        return ((char)(col + 'A')).ToString() + row.ToString();
    }
}
