using UnityEngine;

// 敵が近接か遠距離か、どっちのタイプかを表すやつ
public enum EnemyType
{
    Melee,
    Ranged
}

// 敵1体につき「どっちのタイプか」と、遠距離の子が保ちたい距離を持ってるだけのシンプルなコンポーネント。
// EnemySpawnerが敵を出すときにAddComponentしてtypeを決めてる。
// これが付いてない敵がいたら(付け忘れとか)、EnemyAI側でMelee扱いにして逃げるようにしてある。
public class EnemyKind : MonoBehaviour
{
    public EnemyType type = EnemyType.Melee;
    public int preferredRange = 3; // Ranged系がこの距離を保とうとする(近すぎたら離れて、遠すぎたら詰めてくる)
    public int attackDamage = 10;  // 1回の攻撃のダメージ。ボスはここを上げて強くしてある
}
