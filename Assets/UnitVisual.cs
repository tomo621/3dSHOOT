using UnityEngine;

// ユニットの見た目(3Dモデル)を差し替えるためのスクリプト。
// 元のプリミティブ(Cube)のCollider・タグはゲームロジック側(クリック判定、
// GameObject.FindGameObjectsWithTag("Enemy")とか)で使われてるのでそのまま残して、
// 見た目用のRendererだけ非表示にして、代わりにResourcesフォルダから読み込んだモデルを
// 子オブジェクトとして表示する。プレハブやInspector設定は使わず、PlayerHealth/EnemyHealthの
// Start()からコードで自動的にアタッチしてる。
public class UnitVisual : MonoBehaviour
{
    // Resources/以下のパス(拡張子なし)。例: "Models/PlayerUnit"
    public string modelResourcePath = "";

    // モデルの位置・回転・大きさの微調整用(見た目がズレてたらInspectorで直接調整できる)
    public Vector3 modelOffset = new Vector3(0f, -0.5f, 0f); // Cubeは中心基準(y=0.5が足元)だから、足元(y=0)に合わせて下にずらす
    public Vector3 modelEulerRotation = Vector3.zero;
    public float modelScale = 1f;

    void Start()
    {
        if (string.IsNullOrEmpty(modelResourcePath)) return;

        GameObject modelPrefab = Resources.Load<GameObject>(modelResourcePath);
        if (modelPrefab == null)
        {
            Debug.LogWarning(gameObject.name + ": モデルが見つからない(" + modelResourcePath + ")。元のCubeのまま表示します。");
            return;
        }

        // 元のCubeの見た目(Renderer)だけ隠す。Collider・タグはそのまま残すので、クリック判定とか
        // 既存のゲームロジックには影響しない。
        Renderer cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            cubeRenderer.enabled = false;
        }

        GameObject model = Instantiate(modelPrefab, transform);
        model.transform.localPosition = modelOffset;
        model.transform.localRotation = Quaternion.Euler(modelEulerRotation);
        model.transform.localScale = Vector3.one * modelScale;

        FixMaterialsForURP(model);
        ApplyRangedTintIfNeeded(model);
        PlayIdleAnimation(model);
    }

    // モデルに"idle"って名前のアニメーションクリップが入ってたら、ループ再生する。
    // AnimatorController(Mecanim)はUnityエディタ上でしか作れなくて、コードから自動生成できないので、
    // 代わりにlegacyのAnimationコンポーネントでクリップを直接再生する方式にしてる。
    // これでモデルがTポーズのまま固まるのを防いでる。
    void PlayIdleAnimation(GameObject model)
    {
        // FBXインポート時にAnimatorが付いてる場合、legacyのAnimationと喧嘩するので無効化しておく
        Animator existingAnimator = model.GetComponentInChildren<Animator>();
        if (existingAnimator != null)
        {
            existingAnimator.enabled = false;
        }

        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(modelResourcePath);
        if (clips == null || clips.Length == 0) return;

        AnimationClip idleClip = null;
        foreach (AnimationClip c in clips)
        {
            if (c != null && c.name == "idle")
            {
                idleClip = c;
                break;
            }
        }
        if (idleClip == null) return;

        idleClip.legacy = true;
        idleClip.wrapMode = WrapMode.Loop;

        Animation anim = model.GetComponent<Animation>();
        if (anim == null)
        {
            anim = model.AddComponent<Animation>();
        }
        anim.AddClip(idleClip, idleClip.name);
        anim.clip = idleClip;
        anim.wrapMode = WrapMode.Loop;
        anim.Play(idleClip.name);
    }

    // 遠距離タイプの敵(EnemyKind.type == Ranged)は、元のCubeだとcyan色のマテリアルで
    // 見分けられるようにしてたけど、Cube自体のRendererはここで隠しちゃうので、
    // 新しいモデル側にも同じ色を引き継いでおく。
    void ApplyRangedTintIfNeeded(GameObject model)
    {
        EnemyKind kind = GetComponent<EnemyKind>();
        if (kind == null || kind.type != EnemyType.Ranged) return;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i].color = Color.cyan;
            }
            r.materials = mats;
        }
    }

    // 配布されてる無料モデルはURP(Universal Render Pipeline)用に作られてないことが多くて、
    // そのままだとマテリアルが非対応シェーダーの色(ピンクや黒)になっちゃうことがある。
    // 元のテクスチャだけ引き継いで、URP/Litシェーダーのマテリアルに差し替えることでそれを避けてる。
    void FixMaterialsForURP(GameObject model)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return; // URPのシェーダーが見つからない場合は元のマテリアルのままにする

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Texture mainTex = mats[i].mainTexture;
                Material newMat = new Material(urpLit);
                if (mainTex != null)
                {
                    newMat.mainTexture = mainTex;
                }
                mats[i] = newMat;
            }
            r.materials = mats;
        }
    }
}
