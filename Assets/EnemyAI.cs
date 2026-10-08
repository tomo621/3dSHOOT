using UnityEngine;
using System.Collections;

// 敵AIの行動を専門に担当するクラス。
// 敵の種類(EnemyKindコンポーネント)で行動が分かれる:
//   - Melee(デフォルト。EnemyKindが付いてない敵もこれ扱い): プレイヤーへ1マス近づいて、
//     隣接してたら近接攻撃する
//   - Ranged: preferredRangeを保とうとする。近すぎたら1マス離れて、射程内なら足を止めて遠距離攻撃、
//     射程外なら1マス詰める
// どっちの移動でも、移動先に他の敵・プレイヤー・障害物(GridObstacles)があればそっちへは進まず、
// 空いてる方向を試すか、その場に留まる。
// 移動は瞬間移動じゃなくて、時間をかけて滑らかにスライドさせて、移動方向を向かせる
// (前は瞬間移動だったので「敵が動いてないように見える」っていう見た目の問題があった)。
//
// 元々はGameManager.EnemyTurnに直接書いてあった処理をここに切り出した
// (GameManagerがターン管理・勝敗判定・UI生成・シーン遷移まで全部抱える「God Object」になってたので、
// 敵の行動ロジックだけを責務として分離してある)。
// GameManager.Awakeで自動的にアタッチされて、必要なフィールド(グリッド情報・プレイヤー参照)を渡された上で、
// GameManager.EnemyTurnRoutineからTakeTurnRoutine()を1ターンごとに呼ばれて実行する。
public class EnemyAI : MonoBehaviour
{
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int gridWidth = 10;
    public int gridHeight = 10;
    public GameObject playerUnit;
    public float moveDuration = 0.25f; // 1マス移動にかける時間(秒)。瞬間移動だと動きが分かりにくいので時間をかける。

    // 敵を1体だけ選んで1ターン分の行動(移動+可能なら攻撃)をさせる。GameManager.EnemyTurnRoutineから呼ばれる。
    // 移動アニメーションが終わるまで待つ必要があるので、コルーチンにしてある。
    public IEnumerator TakeTurnRoutine()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0 || playerUnit == null) yield break;

        GameObject mover = enemies[Random.Range(0, enemies.Length)]; // 今回動かす敵をランダムに1体選ぶ

        EnemyKind kind = mover.GetComponent<EnemyKind>();

        if (kind != null && kind.type == EnemyType.Ranged)
        {
            yield return TakeRangedTurnRoutine(mover, kind.preferredRange, enemies);
        }
        else
        {
            yield return TakeMeleeTurnRoutine(mover, enemies);
        }
    }

    // 近接タイプ: プレイヤーへ1マス近づいて、隣接してたら近接攻撃する(元のTakeMeleeTurnと同じ挙動+移動アニメーション)
    IEnumerator TakeMeleeTurnRoutine(GameObject mover, GameObject[] enemies)
    {
        Vector2Int moverGrid = ToGrid(mover.transform.position);
        Vector2Int playerGrid = ToGrid(playerUnit.transform.position);

        Vector2Int newGrid = StepTowardOrAway(mover, moverGrid, playerGrid, enemies, away: false);
        yield return MoveAnimated(mover, moverGrid, newGrid);

        if (mover == null) yield break; // 移動中に消えた場合(普通は起きないけど念のため)

        FaceTarget(mover, playerUnit.transform.position); // 攻撃前にプレイヤーの方を向かせる

        int diffX = Mathf.Abs(newGrid.x - playerGrid.x);
        int diffZ = Mathf.Abs(newGrid.y - playerGrid.y);

        if (diffX <= 1 && diffZ <= 1)
        {
            Debug.Log(mover.name + " attacks (melee)");
            Vector3 targetPos = playerUnit.transform.position;
            int damage = GetAttackDamage(mover);

            AudioFX.PlayMeleeSwing();
            StartCoroutine(AttackFX.MeleeAttack(mover.transform, targetPos, () =>
            {
                StartCoroutine(AttackFX.HitStop(0.06f));
                DealDamageToPlayer(damage);
            }));
        }
        else
        {
            Debug.Log(mover.name + " did not attack (too far)");
        }
    }

    // 遠距離タイプ: preferredRangeを保とうとする。隣接するほど近ければ離れて、射程内かつ近すぎなければ
    // 足を止めて遠距離攻撃、射程外なら1マス詰める。
    IEnumerator TakeRangedTurnRoutine(GameObject mover, int preferredRange, GameObject[] enemies)
    {
        Vector2Int moverGrid = ToGrid(mover.transform.position);
        Vector2Int playerGrid = ToGrid(playerUnit.transform.position);

        int diffX = Mathf.Abs(moverGrid.x - playerGrid.x);
        int diffZ = Mathf.Abs(moverGrid.y - playerGrid.y);

        bool tooClose = (diffX <= 1 && diffZ <= 1);
        bool inPreferredRange = (diffX <= preferredRange && diffZ <= preferredRange);

        Vector2Int newGrid = moverGrid;

        if (tooClose)
        {
            newGrid = StepTowardOrAway(mover, moverGrid, playerGrid, enemies, away: true); // 近すぎるので離れる
        }
        else if (!inPreferredRange)
        {
            newGrid = StepTowardOrAway(mover, moverGrid, playerGrid, enemies, away: false); // 射程外なので詰める
        }
        // 射程内で近すぎもしない場合は、その場に留まって攻撃に専念する(newGridはmoverGridのまま)

        yield return MoveAnimated(mover, moverGrid, newGrid);

        if (mover == null) yield break;

        FaceTarget(mover, playerUnit.transform.position); // 攻撃前にプレイヤーの方を向かせる(足を止めた場合も含む)

        int newDiffX = Mathf.Abs(newGrid.x - playerGrid.x);
        int newDiffZ = Mathf.Abs(newGrid.y - playerGrid.y);
        bool stillTooClose = (newDiffX <= 1 && newDiffZ <= 1);
        bool nowInRange = (newDiffX <= preferredRange && newDiffZ <= preferredRange);

        // 射程内で、かつ隣接するほど近くなってなければ遠距離攻撃する
        // (離れる動きをした直後で、それでもまだ近すぎる場合は今回は攻撃しない)
        if (nowInRange && !stillTooClose)
        {
            Debug.Log(mover.name + " attacks (ranged)");
            Vector3 targetPos = playerUnit.transform.position;
            int damage = GetAttackDamage(mover);

            AudioFX.PlayShoot();
            StartCoroutine(AttackFX.RangedAttack(mover.transform, targetPos, () =>
            {
                DealDamageToPlayer(damage);
            }, 1));
        }
        else
        {
            Debug.Log(mover.name + " repositioned (ranged, no attack this turn)");
        }
    }

    void DealDamageToPlayer(int damage)
    {
        PlayerHealth ph = playerUnit.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
        }
    }

    // この敵のEnemyKind.attackDamageを返す(EnemyKindが付いてなければデフォルト10)。
    // ボスはEnemySpawner.SpawnBossでattackDamageを上げてあるので、ここを通すだけで
    // 自動的に通常の敵より高いダメージになる。
    int GetAttackDamage(GameObject enemy)
    {
        EnemyKind kind = enemy.GetComponent<EnemyKind>();
        return (kind != null) ? kind.attackDamage : 10;
    }

    // moverGridから見てplayerGridの方向へ(away=trueなら逆方向へ)1マス進む移動先を決める。
    // 優先軸(差が大きい方)→予備軸の順で、他のユニット・プレイヤー・障害物がいないマスを探す。
    // 両方塞がってる/動く必要がない場合は元の位置を返す(=その場に留まる)。
    Vector2Int StepTowardOrAway(GameObject mover, Vector2Int moverGrid, Vector2Int playerGrid, GameObject[] enemies, bool away)
    {
        int towardX = playerGrid.x - moverGrid.x;
        int towardZ = playerGrid.y - moverGrid.y;

        if (away)
        {
            towardX = -towardX;
            towardZ = -towardZ;
        }

        int primaryDx = 0, primaryDz = 0;
        int altDx = 0, altDz = 0;

        if (Mathf.Abs(towardX) > Mathf.Abs(towardZ))
        {
            primaryDx = (int)Mathf.Sign(towardX);
            if (towardZ != 0) altDz = (int)Mathf.Sign(towardZ);
        }
        else if (Mathf.Abs(towardZ) > 0)
        {
            primaryDz = (int)Mathf.Sign(towardZ);
            if (towardX != 0) altDx = (int)Mathf.Sign(towardX);
        }

        Vector2Int primary = ClampToGrid(new Vector2Int(moverGrid.x + primaryDx, moverGrid.y + primaryDz));
        Vector2Int alt = ClampToGrid(new Vector2Int(moverGrid.x + altDx, moverGrid.y + altDz));

        bool primaryHasMove = (primary != moverGrid);
        bool altHasMove = (alt != moverGrid);

        if (primaryHasMove && !IsCellOccupied(primary.x, primary.y, mover, enemies, playerGrid.x, playerGrid.y))
        {
            return primary;
        }
        if (altHasMove && !IsCellOccupied(alt.x, alt.y, mover, enemies, playerGrid.x, playerGrid.y))
        {
            return alt;
        }

        return moverGrid; // どっちも動けない場合はその場に留まる
    }

    // ワールド座標→グリッド座標
    Vector2Int ToGrid(Vector3 worldPos)
    {
        int gx = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
        int gz = Mathf.FloorToInt((worldPos.z - gridOrigin.z) / cellSize);
        return new Vector2Int(gx, gz);
    }

    Vector2Int ClampToGrid(Vector2Int grid)
    {
        return new Vector2Int(Mathf.Clamp(grid.x, 0, gridWidth - 1), Mathf.Clamp(grid.y, 0, gridHeight - 1));
    }

    // 指定マスへ、瞬間移動じゃなくて時間をかけて滑らかにスライドさせながら移動する。
    // 移動するときは、動き出す前に進行方向を向かせる(左右上下、見た目で分かるように)。
    // moveDuration秒かけてLerpで移動して、終わるまでこのコルーチンは完了しない
    // (呼び出し側のGameManager.EnemyTurnRoutineが、これの完了を待ってからプレイヤーのターンに戻す)。
    IEnumerator MoveAnimated(GameObject mover, Vector2Int fromGrid, Vector2Int toGrid)
    {
        if (mover == null) yield break;

        Vector3 newPos = new Vector3(
            gridOrigin.x + toGrid.x * cellSize + cellSize / 2f,
            mover.transform.position.y,
            gridOrigin.z + toGrid.y * cellSize + cellSize / 2f
        );

        bool didMove = (toGrid != fromGrid);

        if (!didMove)
        {
            Debug.Log(mover.name + " stayed (blocked or holding position)");
            yield break;
        }

        AudioFX.PlayMove();
        FaceTarget(mover, newPos); // 移動方向(左右上下)を向かせる

        Vector3 startPos = mover.transform.position;
        float t = 0f;

        while (t < moveDuration)
        {
            if (mover == null) yield break; // 移動中に消えた場合(普通は起きないけど念のため)
            t += Time.deltaTime;
            mover.transform.position = Vector3.Lerp(startPos, newPos, t / moveDuration);
            yield return null;
        }

        mover.transform.position = newPos; // 誤差が残らないよう最後にピッタリ位置を合わせとく
        Debug.Log(mover.name + " moved");
    }

    // moverをtargetPosの方向へ向かせる(Y軸回りの回転だけ。見た目のモデルが前後逆とかにズレる場合は
    // UnitVisual.modelEulerRotationで調整できる)
    void FaceTarget(GameObject mover, Vector3 targetPos)
    {
        if (mover == null) return;

        Vector3 dir = targetPos - mover.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            mover.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }

    // 指定したグリッド座標に、他の敵・プレイヤー・障害物のどれかがもういるかどうかを調べる(重なって移動するのを防ぐ用)。
    // mover自身は判定から外す(自分の今いる場所はチェック対象にしない)。
    bool IsCellOccupied(int gridX, int gridZ, GameObject mover, GameObject[] enemies, int playerGridX, int playerGridZ)
    {
        if (gridX == playerGridX && gridZ == playerGridZ) return true;

        if (GridObstacles.GetBlockedCells(PlayerProgress.currentStage, gridWidth, gridHeight).Contains(new Vector2Int(gridX, gridZ)))
        {
            return true;
        }

        foreach (GameObject enemy in enemies)
        {
            if (enemy == mover) continue;

            Vector2Int eGrid = ToGrid(enemy.transform.position);
            if (eGrid.x == gridX && eGrid.y == gridZ) return true;
        }

        return false;
    }
}
