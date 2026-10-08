using UnityEngine;

// マス目の線を描くだけの見た目用クラス(ゲームの判定には一切関係ない)。
// LineRendererを1本ずつ作って、縦線(gridWidth+1本)と横線(gridHeight+1本)を引いてるだけ。
public class GridDrawer : MonoBehaviour
{
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public Color lineColor = Color.white;
    public float lineWidth = 0.02f;

    void Start()
    {
        DrawGrid();
    }

    void DrawGrid()
    {
        // 縦線を引く(x方向に0〜gridWidthまで、Z方向に伸びる線)
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x * cellSize, 0.01f, 0);
            Vector3 end = gridOrigin + new Vector3(x * cellSize, 0.01f, gridHeight * cellSize);
            CreateLine(start, end);
        }

        // 横線を引く(z方向に0〜gridHeightまで、X方向に伸びる線)
        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = gridOrigin + new Vector3(0, 0.01f, z * cellSize);
            Vector3 end = gridOrigin + new Vector3(gridWidth * cellSize, 0.01f, z * cellSize);
            CreateLine(start, end);
        }
    }

    // startからendまで、LineRendererを1本作る
    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.parent = transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.sortingOrder = 1;
    }
}
