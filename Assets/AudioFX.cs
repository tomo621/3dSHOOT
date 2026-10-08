using UnityEngine;

// 効果音をまとめた静的クラス。
// 音声ファイル(アセット)は一切使わないで、AudioClip.Createで単純な音(サイン波+減衰エンベロープ)を
// その場で合成して鳴らす。ScenePolishが画像アセット無しで見た目を作ってるのと同じ考え方。
// 呼び出し側はPlayXxx()を呼ぶだけでOK(作ったAudioSourceは鳴り終わったら自動で消える)。
public static class AudioFX
{
    const int SampleRate = 44100;

    private static AudioSource bgmSource; // 再生中の通常BGM(重複再生を防ぐために持っておく)
    private static bool bgmIsBossStage;   // 再生中のBGMがボス用かどうか(種類が変わったときだけ鳴らし直すための記録)

    // 攻撃が当たった瞬間の短いヒット音(敵がダメージを受けたときに使う)
    public static void PlayHit()
    {
        Play(GenerateTone(220f, 0.08f, 0.9f), 0.6f);
    }

    // 近接攻撃を繰り出す瞬間の、素早く踏み込むような音。
    // 遠距離攻撃の「ピュン」って音とは違って、短く低く落ちる音にすることで、
    // 音だけでも近接攻撃だって分かるようにしてる。
    public static void PlayMeleeSwing()
    {
        Play(GenerateSweep(700f, 180f, 0.09f), 0.55f);
    }

    // ユニットが1マス移動するときの短い音(プレイヤー・敵どっちの移動でも使う)。
    // 前は単純なサイン波(ピッという電子音)だったけど、足音っぽくするために
    // 低い打撃音+ノイズ成分を組み合わせた短い「ドッ」って音に変えた。
    public static void PlayMove()
    {
        Play(GenerateFootstep(), 0.5f);
    }

    // 遠距離攻撃の弾を発射する瞬間の「ピュン」って音。
    // 着弾時の効果音(PlayHit)とは別に、撃った瞬間にも音を鳴らすことで、
    // 近接攻撃(PlayMeleeSwing)との違いが音だけでも分かるようにしてる。
    public static void PlayShoot()
    {
        Play(GenerateSweep(1500f, 700f, 0.07f), 0.5f);
    }

    // ボス撃破による「Game Clear」のときの、通常のステージクリアより長くて豪華なファンファーレ
    public static void PlayGameClear()
    {
        Play(GenerateArpeggio(new float[] { 392f, 523f, 659f, 784f, 1046f, 784f, 1046f }, 0.14f), 0.65f);
    }

    // 敵を撃破したときの、少し下がっていく音
    public static void PlayEnemyDeath()
    {
        Play(GenerateSweep(440f, 110f, 0.25f), 0.5f);
    }

    // プレイヤーがダメージを受けたときの、少し低めの音
    public static void PlayPlayerDamage()
    {
        Play(GenerateTone(160f, 0.15f, 0.8f), 0.6f);
    }

    // レベルアップ選択画面が出るときの、上がっていく3音
    public static void PlayLevelUp()
    {
        Play(GenerateArpeggio(new float[] { 523f, 659f, 784f }, 0.09f), 0.5f);
    }

    // ステージクリア時の、明るい4音
    public static void PlayStageClear()
    {
        Play(GenerateArpeggio(new float[] { 392f, 523f, 659f, 784f }, 0.12f), 0.5f);
    }

    // 敗北したときの、低く落ちていく音
    public static void PlayLose()
    {
        Play(GenerateSweep(300f, 80f, 0.5f), 0.6f);
    }

    // プレイ中ずっとループするBGMを再生開始する(もう再生中なら何もしない)。
    // isBossStage: trueならボス戦用の、テンポが速くて緊迫感のあるBGMにする(通常ステージとは別の曲)。
    // 他の効果音と違ってずっと鳴り続ける必要があるので、専用のAudioSourceを持っておいて、
    // 自動で消す処理(時間指定のObject.Destroy)はしない。
    public static void PlayBGM(bool isBossStage = false)
    {
        if (bgmSource != null)
        {
            // もう同じ種類(通常orボス)のBGMを再生中なら何もしない(鳴らし直して途切れさせない)
            if (bgmIsBossStage == isBossStage) return;

            // 種類が変わる場合(通常→ボス、ボス→通常)は一旦止めてから新しい方を鳴らし直す。
            // 本来はEndGameのStopBGMで毎回ちゃんと止めてからPlayBGMが呼ばれるはずだけど、
            // 万が一前のBGMが残ったままだった場合でも、ここで確実に正しい方へ切り替わるようにしてある。
            StopBGM();
        }

        // 「BGMだけ何も聞こえない」って報告があったので、ここで万一例外が起きて
        // 無音のまま失敗してないかをConsoleで確認できるように、try/catchとログを入れてある。
        try
        {
            // シーンにAudioListenerが1つも無いと、どのAudioSourceを鳴らしても音が出ない
            // (Unity側が警告を出すだけで止まりはしないので気づきにくい)。ここで明示的にチェックして、
            // 無ければConsoleにはっきり分かるログを出す。
            if (Object.FindFirstObjectByType<AudioListener>() == null)
            {
                Debug.LogWarning("AudioFX: シーンにAudioListenerが見つかりません。これが原因でBGM/SEが一切聞こえていない可能性があります。");
            }

            GameObject go = new GameObject("BGM_Player");
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = isBossStage ? GenerateBossBGMLoop() : GenerateBGMLoop();
            source.loop = true;
            // 0.6/0.7でもまだ聞き取れないって話だったので、通常/ボスどっちも効果音の最大音量と
            // 同じ0.8fに揃えた。本番ビルドでもこの数値がそのまま使われる
            // (Windowsの音量ミキサーとかとは関係なく、ここの数値だけでプレイヤーに届く音量が決まる)。
            source.volume = 0.8f;
            source.spatialBlend = 0f;
            source.Play();

            bgmSource = source;
            bgmIsBossStage = isBossStage;

            // 実際に再生が始まったことをConsoleで確認できるようにしておく
            // (このログが出てなければ、PlayBGM自体が呼ばれてないかここで止まってる)
            Debug.Log("AudioFX: BGM再生開始(" + (isBossStage ? "ボス戦用" : "通常用") + ") volume=" + source.volume
                + " isPlaying=" + source.isPlaying);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    // 通常BGMを止める(勝敗が決まったときにGameManagerから呼ばれる)
    public static void StopBGM()
    {
        if (bgmSource != null)
        {
            Object.Destroy(bgmSource.gameObject);
            bgmSource = null;
        }
    }

    // 作ったAudioClipを一時的なGameObjectで再生して、鳴り終わったら自動で消す
    static void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameObject go = new GameObject("AudioFX");
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f; // 2D再生(位置による減衰なし。どこで鳴った音でも同じ音量で聞こえる)
        source.Play();

        Object.Destroy(go, clip.length + 0.1f);
    }

    // 単音(指定した周波数・長さで、最後にかけて減衰していくサイン波)
    static AudioClip GenerateTone(float frequency, float duration, float decayPower)
    {
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Pow(1f - (float)i / sampleCount, decayPower);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.8f;
        }

        AudioClip clip = AudioClip.Create("AudioFX_Tone", sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // 周波数がstartからendへ変わっていく、短いスイープ音(撃破・敗北とかに使う)
    static AudioClip GenerateSweep(float startFreq, float endFreq, float duration)
    {
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
        float[] data = new float[sampleCount];

        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float progress = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, progress);
            phase += freq / SampleRate;

            float envelope = 1f - progress;
            data[i] = Mathf.Sin(2f * Mathf.PI * phase) * envelope * 0.8f;
        }

        AudioClip clip = AudioClip.Create("AudioFX_Sweep", sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // 足音用の短い打撃音。低い周波数のサイン波(衝撃音の芯)とノイズ(ざらつき)を混ぜて、
    // ただの電子音じゃなく「地面に足が着いた」ような質感にしてる。急激に減衰させることで、
    // 伸びのある音じゃなくて短く締まった打撃音にしてる。
    static AudioClip GenerateFootstep()
    {
        float duration = 0.09f;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Pow(1f - (float)i / sampleCount, 3f);
            float thump = Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.6f;
            float noise = Random.Range(-1f, 1f) * 0.4f;
            data[i] = (thump + noise) * envelope * 0.7f;
        }

        AudioClip clip = AudioClip.Create("AudioFX_Footstep", sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // 複数の音を順番に短く鳴らす(レベルアップ・ステージクリアの「ピロン」って音に使う)
    static AudioClip GenerateArpeggio(float[] frequencies, float noteDuration)
    {
        int noteSamples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * noteDuration));
        int totalSamples = noteSamples * frequencies.Length;
        float[] data = new float[totalSamples];

        for (int n = 0; n < frequencies.Length; n++)
        {
            float freq = frequencies[n];
            for (int i = 0; i < noteSamples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Pow(1f - (float)i / noteSamples, 0.7f);
                data[n * noteSamples + i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.8f;
            }
        }

        AudioClip clip = AudioClip.Create("AudioFX_Arpeggio", totalSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // プレイ中にループ再生する通常BGM用のフレーズを合成する。
    // 前はベースの単音だけで「地味・小さい」って指摘があったので、
    // ベース(低音の芯)+キック(拍の頭を強調する打撃音)+ハイハット(裏拍のノイズ)+
    // リード(高音の彩り)の4層を重ねて、曲らしい厚みとリズムの推進力を持たせてる。
    // どの層も各音の最後でしっかり0近くまで減衰するので、loop=trueでつなげてもブツ切れにならない。
    static AudioClip GenerateBGMLoop()
    {
        float[] bassNotes = { 220f, 220f, 262f, 220f, 196f, 196f, 233f, 220f };
        float[] leadNotes = { 440f, 523f, 440f, 523f, 392f, 466f, 392f, 440f }; // ベースの1オクターブ上で動く簡単なリード
        float noteDuration = 0.35f;

        int noteSamples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * noteDuration));
        int totalSamples = noteSamples * bassNotes.Length;
        float[] data = new float[totalSamples];

        for (int n = 0; n < bassNotes.Length; n++)
        {
            float bassFreq = bassNotes[n];
            float leadFreq = leadNotes[n % leadNotes.Length];

            for (int i = 0; i < noteSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = (float)i / noteSamples;

                // ベース: 基音+1オクターブ上の弱い成分
                float bassEnv = Mathf.Pow(1f - progress, 1.5f);
                float bass = (Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.7f
                            + Mathf.Sin(2f * Mathf.PI * bassFreq * 2f * t) * 0.15f) * bassEnv * 0.4f;

                // キック: 各音の頭だけ鳴る低い打撃音(リズムの芯)
                float kickEnv = Mathf.Pow(Mathf.Max(0f, 1f - progress * 6f), 2f);
                float kick = Mathf.Sin(2f * Mathf.PI * 65f * t) * kickEnv * 0.45f;

                // ハイハット: 各音の裏拍(中間点)だけ鳴る短いノイズ(裏打ちでリズムに動きを出す)
                float hatPhase = progress - 0.5f;
                float hatEnv = (hatPhase >= 0f) ? Mathf.Pow(Mathf.Max(0f, 1f - hatPhase * 10f), 2f) : 0f;
                float hat = Random.Range(-1f, 1f) * hatEnv * 0.12f;

                // リード: ベースより高い音を薄く重ねて、単調なベースラインに彩りを足す
                float leadEnv = Mathf.Pow(1f - progress, 2f) * 0.15f;
                float lead = Mathf.Sin(2f * Mathf.PI * leadFreq * t) * leadEnv;

                data[n * noteSamples + i] = bass + kick + hat + lead;
            }
        }

        AudioClip clip = AudioClip.Create("AudioFX_BGM", totalSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ボス戦用BGM。通常BGMと同じ4層構成(ベース+キック+ハイハット+リード)だけど、
    // 音域を低く・テンポを速くして、キックを強めに・ハイハットを倍速にすることで
    // 通常版よりドスドスした圧と緊迫感を出してる。
    static AudioClip GenerateBossBGMLoop()
    {
        float[] bassNotes = { 130f, 130f, 155f, 130f, 110f, 130f, 155f, 174f };
        float[] leadNotes = { 260f, 310f, 260f, 349f, 220f, 260f, 310f, 349f };
        float noteDuration = 0.22f; // 通常BGM(0.35秒)より速いテンポ

        int noteSamples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * noteDuration));
        int totalSamples = noteSamples * bassNotes.Length;
        float[] data = new float[totalSamples];

        for (int n = 0; n < bassNotes.Length; n++)
        {
            float bassFreq = bassNotes[n];
            float leadFreq = leadNotes[n % leadNotes.Length];

            for (int i = 0; i < noteSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = (float)i / noteSamples;

                // ベース: オクターブ下の成分を強めに混ぜて、通常BGMより重く・低く響かせる
                float bassEnv = Mathf.Pow(1f - progress, 1.2f);
                float bass = (Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.6f
                            + Mathf.Sin(2f * Mathf.PI * bassFreq * 0.5f * t) * 0.3f) * bassEnv * 0.35f;

                // キックは通常版より強め・速めにして、ドスドスした圧を出す
                float kickEnv = Mathf.Pow(Mathf.Max(0f, 1f - progress * 8f), 2f);
                float kick = Mathf.Sin(2f * Mathf.PI * 60f * t) * kickEnv * 0.4f;

                // ハイハットは1音につき2回(表と裏)鳴らして、速く刻む緊迫感を出す
                float hatPos = (progress * 2f) % 1f;
                float hatEnv = Mathf.Pow(Mathf.Max(0f, 1f - hatPos * 12f), 2f);
                float hat = Random.Range(-1f, 1f) * hatEnv * 0.14f;

                float leadEnv = Mathf.Pow(1f - progress, 2.5f) * 0.25f;
                float lead = Mathf.Sin(2f * Mathf.PI * leadFreq * t) * leadEnv;

                data[n * noteSamples + i] = bass + kick + hat + lead;
            }
        }

        AudioClip clip = AudioClip.Create("AudioFX_BossBGM", totalSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
