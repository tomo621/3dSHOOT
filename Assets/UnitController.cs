using UnityEngine;

// ユニットを目標の座標までなめらかに動かすコントローラ。
// 今はプレイヤー側(クリックで移動させる方)でしか使ってなくて、
// 敵の方はこれを使わずにGameManager.EnemyTurnの中で直接position書き換えて瞬間移動させてるので注意。
public class UnitController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f; // 1秒で回れる最大角度(度)。進む方向にくるっと向きを合わせる用
    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        // 目標地点に向かって動かす
        if (isMoving)
        {
            // 進む方向にちゃんと向きを合わせる。Y軸回転だけなので上下に動いても傾いたりはしない
            Vector3 dir = targetPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            // 目標地点に十分近づいたら止める(ピッタリ位置を合わせて、移動中フラグをオフにする)
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    // 移動の指示を受け取る(destinationに向かってUpdateの中で少しずつ動き出す)
    public void MoveTo(Vector3 destination)
    {
        targetPosition = destination;
        isMoving = true;
    }
}
