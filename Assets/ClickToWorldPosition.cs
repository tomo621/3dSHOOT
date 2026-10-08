using UnityEngine;
using System.Collections;

// プレイヤーの入力(クリック)を処理する中心的なクラス。InputManagerにアタッチしてある。
// 自分のターンじゃなければ何もしない → クリックした位置をレイキャストで判定 →
//   敵をクリック: 攻撃範囲内なら近接/遠距離を自動で判定して攻撃、ターン終了
//   地面をクリック: 移動範囲内ならそこへ移動して、移動範囲・攻撃範囲のハイライトを更新。
//                 移動後に敵が攻撃範囲内にいなければそのままターン終了。
public class ClickToWorldPosition : MonoBehaviour
{
    public float cellSize = 1.0f;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);

    public UnitController unit;              // 操作対象(プレイヤー)のユニット
    public GameObject hitEffectPrefab;        // 攻撃が当たったときのエフェクト
    public GameObject damagePopupPrefab;      // ダメージ数値のポップアップ
    public AttackRangeShower rangeShower;     // 移動できる範囲の表示
    public AttackRangeShower2 attackRangeShower; // 攻撃できる範囲の表示
    public int attackRange = 1;               // 攻撃が届く距離(マス数)

    void Update()
    {
        // 自分のターンじゃない間はクリックを受け付けない
        if (GameManager.Instance != null && !GameManager.Instance.IsPlayerTurn())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // --- 敵をクリックした場合: 攻撃 ---
                if (hit.collider.CompareTag("Enemy"))
                {
                    int unitGridX = Mathf.FloorToInt((unit.transform.position.x - gridOrigin.x) / cellSize);
                    int unitGridZ = Mathf.FloorToInt((unit.transform.position.z - gridOrigin.z) / cellSize);

                    int enemyGridX = Mathf.FloorToInt((hit.collider.transform.position.x - gridOrigin.x) / cellSize);
                    int enemyGridZ = Mathf.FloorToInt((hit.collider.transform.position.z - gridOrigin.z) / cellSize);

                    int diffX = Mathf.Abs(unitGridX - enemyGridX);
                    int diffZ = Mathf.Abs(unitGridZ - enemyGridZ);

                    // attackRangeより遠い敵には攻撃できない
                    if (diffX > attackRange || diffZ > attackRange)
                    {
                        Debug.Log("too far");
                        ShowRejectedClick(hit.point);
                        return;
                    }

                    Transform enemyTransform = hit.collider.transform;
                    Vector3 enemyPos = enemyTransform.position;

                    // 隣接してたら(diffX,diffZが両方1以下)近接攻撃、そうでなければ遠距離攻撃
                    // これが「3DSHOOT」の名前の由来でもある、距離による攻撃の出し分け部分
                    bool isAdjacent = (diffX <= 1 && diffZ <= 1);

                    if (isAdjacent)
                    {
                        AudioFX.PlayMeleeSwing();
                        StartCoroutine(AttackFX.MeleeAttack(unit.transform, enemyPos, () =>
                        {
                            ApplyAttackDamage(enemyTransform, enemyPos);
                        }));
                    }
                    else
                    {
                        // レベルアップで「球の数」を強化してると、2発以上同時に飛ばす
                        int bulletCount = 1 + PlayerProgress.bonusBulletCount;

                        AudioFX.PlayShoot();
                        StartCoroutine(AttackFX.RangedAttack(unit.transform, enemyPos, () =>
                        {
                            ApplyAttackDamage(enemyTransform, enemyPos, bulletCount);
                        }, bulletCount));
                    }

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.EndPlayerTurn();
                    }

                    // 敵のターンに入るので、移動範囲(青)・攻撃範囲(赤)のハイライトは消しておく。
                    // 消さずに残すと、相手のターン中もマス目が青/赤で覆われたままになって見づらい。
                    if (rangeShower != null) rangeShower.ClearRange();
                    if (attackRangeShower != null) attackRangeShower.ClearRange();

                    return;
                }

                // --- 地面をクリックした場合: 移動 ---
                Vector3 clickPosition = hit.point;

                int gridX = Mathf.FloorToInt((clickPosition.x - gridOrigin.x) / cellSize);
                int gridZ = Mathf.FloorToInt((clickPosition.z - gridOrigin.z) / cellSize);

                // 障害物マスには移動できない(移動範囲が表示されてるかどうかに関わらず常にチェックする)
                if (GridObstacles.GetBlockedCells(PlayerProgress.currentStage, gridWidth, gridHeight).Contains(new Vector2Int(gridX, gridZ)))
                {
                    Debug.Log("blocked cell");
                    ShowRejectedClick(hit.point);
                    return;
                }

                // 移動範囲が表示中で、かつクリック位置がその範囲外なら無視
                if (rangeShower != null && rangeShower.HasActiveRange() && !rangeShower.IsValidCell(gridX, gridZ))
                {
                    Debug.Log("range NG");
                    ShowRejectedClick(hit.point);
                    return;
                }

                float destX = gridOrigin.x + gridX * cellSize + cellSize / 2f;
                float destZ = gridOrigin.z + gridZ * cellSize + cellSize / 2f;
                Vector3 destination = new Vector3(destX, 0.5f, destZ);

                if (unit != null)
                {
                    AudioFX.PlayMove();
                    unit.MoveTo(destination);
                }

                // 移動先を基準に、次に選べる移動範囲・攻撃範囲のハイライトを更新する
                if (rangeShower != null)
                {
                    rangeShower.ShowRange(destination);
                }

                if (attackRangeShower != null)
                {
                    attackRangeShower.ShowAttackRange(destination);
                }

                // 移動後の位置から攻撃範囲内に敵がいなければ、その場でターンを終わらせる
                // (敵がいればプレイヤーはそのまま攻撃を選べるようにターンを渡さない)
                bool enemyInRange = false;
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (GameObject enemy in enemies)
                {
                    int enemyGridX2 = Mathf.FloorToInt((enemy.transform.position.x - gridOrigin.x) / cellSize);
                    int enemyGridZ2 = Mathf.FloorToInt((enemy.transform.position.z - gridOrigin.z) / cellSize);

                    int diffX2 = Mathf.Abs(gridX - enemyGridX2);
                    int diffZ2 = Mathf.Abs(gridZ - enemyGridZ2);

                    if (diffX2 <= attackRange && diffZ2 <= attackRange)
                    {
                        enemyInRange = true;
                        break;
                    }
                }

                if (!enemyInRange && GameManager.Instance != null)
                {
                    GameManager.Instance.EndPlayerTurn();

                    // 敵のターンに入るので、移動範囲(青)・攻撃範囲(赤)のハイライトは消しておく。
                    if (rangeShower != null) rangeShower.ClearRange();
                    if (attackRangeShower != null) attackRangeShower.ClearRange();
                }
            }
        }
    }

    // 自分のターンが始まった瞬間(ゲーム開始時・敵のターンが終わった直後)にGameManagerから呼ばれる。
    // 今までは実際に1回クリックして動くまで移動範囲(青)・攻撃範囲(赤)が一切表示されなくて、
    // 「今どこまで動けて、どの敵が攻撃範囲に入ってるか」がターン開始直後は分からなかった。
    // ここで現在地を基準に両方のハイライトを先に出しておくことで、クリックする前からひと目で分かるようにする。
    public void ShowRangesAtCurrentPosition()
    {
        if (unit == null) return;

        if (rangeShower != null) rangeShower.ShowRange(unit.transform.position);
        if (attackRangeShower != null) attackRangeShower.ShowAttackRange(unit.transform.position);
    }

    // 敵へのダメージ適用処理。AttackFXの演出(着弾/踏み込み)のonImpactコールバックから呼ばれる。
    // hitMultiplier: 遠距離攻撃で「球の数」が強化されてるとき、球の数だけダメージを倍にする(近接攻撃は常に1)。
    void ApplyAttackDamage(Transform enemyTransform, Vector3 enemyPos, int hitMultiplier = 1)
    {
        StartCoroutine(AttackFX.HitStop(0.06f)); // ヒットの瞬間に一瞬止める演出

        EnemyHealth enemyHealth = enemyTransform.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            int damage = 20 * hitMultiplier;
            enemyHealth.TakeDamage(damage);

            if (damagePopupPrefab != null)
            {
                Vector3 popupPos = enemyPos + Vector3.up * 1.5f;
                GameObject popup = Instantiate(damagePopupPrefab, popupPos, Quaternion.identity);
                popup.GetComponent<DamagePopup>().Setup(damage);
            }
        }

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, enemyPos, Quaternion.identity);
        }
    }

    // クリックが無効だったとき(障害物・移動範囲外・攻撃範囲外)に、クリックした位置に赤い印を
    // 一瞬出して、縮みながら消える演出を出す。前はDebug.Logだけで、プレイ画面上には何の反応も
    // 出てなかったので、「クリックしたのに何も起きない」=「たまに動かない」ように見えてしまっていた。
    void ShowRejectedClick(Vector3 worldPos)
    {
        StartCoroutine(RejectedClickFlash(worldPos));
    }

    IEnumerator RejectedClickFlash(Vector3 worldPos)
    {
        // Quad(板ポリゴン)は裏面が描画されないので、回転の向き次第では真上からのカメラから見たとき
        // 裏側が向いてしまって「見えてるはずなのに実際は表示されない」ことがある。
        // Sphereを平たく潰して使えば、どの角度のカメラから見ても必ず見えるので確実。
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "RejectedClickMark";

        Collider col = marker.GetComponent<Collider>();
        if (col != null) Destroy(col); // クリック判定の邪魔にならないよう外しておく

        marker.transform.position = worldPos + Vector3.up * 0.15f;

        Renderer rend = marker.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = new Color(1f, 0.15f, 0.15f, 1f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION"); // 光らせて、床の色に埋もれず目立つようにする
            mat.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.1f) * 2f);
        }
        rend.material = mat;

        float duration = 0.3f;
        float t = 0f;
        Vector3 startScale = new Vector3(0.9f, 0.15f, 0.9f); // 平たい円盤状にする

        while (t < duration)
        {
            if (marker == null) yield break;
            t += Time.deltaTime;
            float k = Mathf.Lerp(1f, 0f, t / duration);
            marker.transform.localScale = startScale * k;
            yield return null;
        }

        Destroy(marker);
    }
}
