using System;
using System.Collections;
using UnityEngine;

// 攻撃の演出(モーションとかエフェクト)をまとめた静的クラス。
// 攻撃する側(ClickToWorldPosition / GameManager)からコルーチンとして呼び出す感じ。
// どの攻撃も「演出が終わったタイミングでonImpactを呼ぶ」って形にしてあるので、
// ダメージ計算やポップアップ表示みたいな実際の処理は呼び出し元(onImpactの中)でやってもらう。
public static class AttackFX
{
    // 近接攻撃: targetの方へ一瞬踏み込んで戻る。踏み込みきったタイミングでonImpactを呼ぶ
    // attacker: 攻撃する側のTransform / targetPos: 攻撃対象の座標 / onImpact: 踏み込みきった瞬間に呼ばれる処理(ダメージ適用とか)
    public static IEnumerator MeleeAttack(Transform attacker, Vector3 targetPos, Action onImpact)
    {
        Vector3 startPos = attacker.position;
        Vector3 dir = targetPos - startPos;
        dir.y = 0f;
        Vector3 lungePos = startPos + dir.normalized * 0.4f; // targetの方向に0.4だけ踏み込んだ位置

        float duration = 0.1f;
        float t = 0f;

        // 踏み込む(startPos -> lungePos)
        while (t < duration)
        {
            t += Time.deltaTime;
            attacker.position = Vector3.Lerp(startPos, lungePos, t / duration);
            yield return null;
        }

        onImpact?.Invoke(); // 踏み込みきった瞬間がヒットのタイミング

        // 元の位置に戻る(lungePos -> startPos)
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            attacker.position = Vector3.Lerp(lungePos, startPos, t / duration);
            yield return null;
        }

        attacker.position = startPos; // 誤差が残らないよう最後にピッタリ位置を合わせとく
    }

    // 遠距離攻撃: attackerの位置からtargetへ光る弾を飛ばす。着弾したらonImpactを呼ぶ
    // 隣接してない敵を攻撃したときはこっちが呼ばれる(ClickToWorldPosition側でisAdjacentで分岐してる)
    // bulletCount: レベルアップで「球の数」を強化してると2発以上になる(見た目だけ複数発、横に並べて飛ばす)。
    // ダメージ自体はClickToWorldPosition.ApplyAttackDamage側でbulletCount分まとめて計算してるので、
    // onImpactはここでは1回だけ呼ぶ。
    public static IEnumerator RangedAttack(Transform attacker, Vector3 targetPos, Action onImpact, int bulletCount = 1)
    {
        if (bulletCount < 1) bulletCount = 1;

        Vector3 startPos = attacker.position + Vector3.up * 0.5f;
        Vector3 endPos = targetPos + Vector3.up * 0.5f;
        Vector3 dir = (endPos - startPos).normalized;
        Vector3 side = Vector3.Cross(dir, Vector3.up); // 複数発を横にばらけさせるための軸

        GameObject[] bullets = new GameObject[bulletCount];
        Vector3[] bulletStartPos = new Vector3[bulletCount];

        for (int i = 0; i < bulletCount; i++)
        {
            // 弾は見た目だけのオブジェクト。当たり判定はダメージ計算側(ApplyAttackDamage)で別にやってるので、
            // Colliderはあると邪魔になるのでここで外してる。
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "Bullet";

            Collider col = bullet.GetComponent<Collider>();
            if (col != null)
            {
                UnityEngine.Object.Destroy(col);
            }

            bullet.transform.localScale = Vector3.one * 0.25f;

            // 光るマテリアルをその場で作って、弾っぽい見た目にする
            Renderer rend = bullet.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.cyan;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.cyan * 3f);
                rend.material = mat;
            }

            // 2発以上あるときは発射位置を少し横にずらして並べる(1発だけなら中心のまま)
            float spread = (bulletCount > 1) ? (i - (bulletCount - 1) / 2f) * 0.3f : 0f;
            bullets[i] = bullet;
            bulletStartPos[i] = startPos + side * spread;
        }

        float duration = 0.15f;
        float t = 0f;

        // startPosからendPosへ一直線に移動させる(複数発あれば全部同時に動かす)
        while (t < duration)
        {
            t += Time.deltaTime;
            for (int i = 0; i < bulletCount; i++)
            {
                if (bullets[i] != null)
                {
                    bullets[i].transform.position = Vector3.Lerp(bulletStartPos[i], endPos, t / duration);
                }
            }
            yield return null;
        }

        for (int i = 0; i < bulletCount; i++)
        {
            if (bullets[i] != null)
            {
                UnityEngine.Object.Destroy(bullets[i]); // 着弾したら弾自体は消しとく
            }
        }

        onImpact?.Invoke(); // 着弾のタイミングがヒットのタイミング(複数発あっても呼ぶのは1回だけ)
    }

    // ヒットストップ: 攻撃が当たった瞬間だけゲームを一瞬止めて、手応えを出す演出。
    // Time.timeScaleを0にするとUpdateの中のTime.deltaTime頼みの動きが全部止まるので、
    // WaitForSecondsRealtime(timeScaleの影響を受けない待機)でduration秒だけ待ってから元に戻す。
    public static IEnumerator HitStop(float duration)
    {
        float originalScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalScale;
    }
}
