using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Spawner : MonoBehaviour
{
    public event EventHandler<OnClickChushouEmergeEventArgs> OnClickChushouEmerge;
    public class OnClickChushouEmergeEventArgs : EventArgs
    {
        public Vector3 clickCubeEndPosition;
        public int clickMusicBlockIndex;
        public int blockCount;
    }

    public event EventHandler<OnSwipeChushouEmergeEventArgs> OnSwipeChushouEmerge;
    public class OnSwipeChushouEmergeEventArgs : EventArgs
    {
        public Vector3 swipeCubeStartPosition;
        public Vector3 swipeCubeEndPosition;
        public int swipeMusicBlockStartIndex;
        public int swipeMusicBlockEndIndex;
        public int blockCount;
    }

    public string csvfile;
    public string goldencsvfile;

    public AudioSource musicSource;
    public GameObject clickPrefab;
    public GameObject clickhintPrefab;
    public GameObject swipePrefab;
    public GameObject swipehintPrefab;
    public MusicBlock[] musicBlocks;
    public List<int> swipeBlocks = new List<int>();

    public AudioFeedback audioFeedback;

    private int nextIndex = 0;
    public float hintDuration { get; private set; } = 4 ;
    static private int sumBlockCount = 0;

    void Start()
    {
        StartCoroutine(LoadMusicBlocksCoroutine(Path.Combine(Application.streamingAssetsPath, csvfile), isGolden: false));
        StartCoroutine(LoadMusicBlocksCoroutine(Path.Combine(Application.streamingAssetsPath, goldencsvfile), isGolden: true));

        AudioClip originalClip = musicSource.clip;
        AudioClip newClip = CreateClipWithSilence(originalClip, hintDuration);
        musicSource.clip = newClip;

    }

    void Update()
    {
        while (nextIndex < musicBlocks.Length && musicSource.time >= musicBlocks[nextIndex].starttime)
        {

            if (musicBlocks[nextIndex].type == 0)
            {
                // 生成click方块
                float lifespan = musicBlocks[nextIndex].lifespan;

                // 针对这个click方块开启一个新线程
                StartCoroutine(Click(nextIndex, lifespan));

            }

            if (musicBlocks[nextIndex].type == 1)
            {
                // 记录swipe方块
                swipeBlocks.Add(nextIndex);
            }

            nextIndex++;
        }

        // 针对swipe开启新线程
        if (swipeBlocks.Count > 0)
        {
            float swipespan = musicBlocks[swipeBlocks[0]].lifespan;
            StartCoroutine(Swipe(swipeBlocks[0], swipeBlocks[swipeBlocks.Count - 1], swipespan));
        }

        swipeBlocks.Clear();

    }

    public MusicBlock[] LoadMusicBlocks(string filePath)
    {

        List<MusicBlock> musicBlocks = new List<MusicBlock>();
        string[] lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++) // 跳过表头行
        {
            string[] values = lines[i].Split(',');

            MusicBlock block = new MusicBlock
            {
                starttime = float.Parse(values[0]),
                gridYaw = int.Parse(values[1]),
                gridPitch = int.Parse(values[2]),
                lifespan = float.Parse(values[3]),
                type = int.Parse(values[4]),
                pitch = int.Parse(values[5]),

            };

            musicBlocks.Add(block);
        }

        return musicBlocks.ToArray();
    }

    private IEnumerator LoadMusicBlocksCoroutine(string filePath, bool isGolden)
    {
        UnityWebRequest request = UnityWebRequest.Get(filePath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string[] lines = request.downloadHandler.text.Split('\n');

            List<MusicBlock> blocksList = new List<MusicBlock>();

            // 跳过表头行
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');

                if (values.Length >= 6) // 确保至少有6列数据
                {
                    MusicBlock block = new MusicBlock
                    {
                        starttime = float.Parse(values[0]),
                        gridYaw = int.Parse(values[1]),
                        gridPitch = int.Parse(values[2]),
                        lifespan = float.Parse(values[3]),
                        type = int.Parse(values[4]),
                        pitch = int.Parse(values[5]),
                    };

                    blocksList.Add(block);
                }
            }

            musicBlocks = blocksList.ToArray();
        }
        else
        {
            Debug.LogError("Failed to load CSV file: " + request.error);
        }
    }

    private GameObject InstantiateMusicBlock(GameObject Prefab, int index)
    {
        Vector3 position = musicBlocks[index].GetPosition();
        Vector3 direction = position - Vector3.zero; // 从原点到方块位置的方向
        Quaternion rotation = Quaternion.LookRotation(direction); // 计算旋转

        GameObject newBlock = Instantiate(Prefab, position, Quaternion.identity);

        return newBlock;
    }

    private GameObject InstantiateHintBlock(GameObject Prefab, int index)
    {
        Vector3 position = musicBlocks[index].GetPosition();
        Vector3 direction = position - Vector3.zero; // 从原点到方块位置的方向
        Quaternion rotation = Quaternion.LookRotation(direction); // 计算旋转
        Vector3 offset = new Vector3(0, 0, 5);
        // 设置到提示的位置
        position = position + offset;

        GameObject newBlock = Instantiate(Prefab, position, Quaternion.identity);

        return newBlock;
    }


    private IEnumerator Hint(GameObject prefab, int hintIndex, float duration)
    {
        GameObject hintBlock = InstantiateHintBlock(prefab, hintIndex);

        Vector3 targetPosition = musicBlocks[hintIndex].GetPosition();
        Vector3 startPosition = hintBlock.transform.position;

        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            hintBlock.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(hintBlock); // 最后删除
    }


    private bool IstentacleCollidingWithBlock(List<Collider> tentacleColliders, GameObject block)
    {
        Collider blockCollider = block.GetComponent<Collider>();

        if (blockCollider == null)
        {
            return false;
        }

        foreach (Collider tentacleCollider in tentacleColliders)
        {
            // make sure this collider is enabled
            if (tentacleCollider.enabled)
            {

                blockCollider.isTrigger = false;
                tentacleCollider.isTrigger = false;

                if (blockCollider.bounds.Intersects(tentacleCollider.bounds))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private IEnumerator Click(int Index, float lifespan)
    {
        sumBlockCount ++;
        int blockCount = sumBlockCount;
        // 启动 Hint 协程
        StartCoroutine(Hint(clickhintPrefab, Index, hintDuration));

        // 等待
        yield return new WaitForSeconds(hintDuration);

        GameObject newBlock = InstantiateMusicBlock(clickPrefab, Index);

        // 记录协程开始时间
        float creationTime = Time.time;
        Destroy(newBlock, lifespan);

        while (newBlock != null)
        {

            if (IstentacleCollidingWithBlock(InputManager.Instance.stickColliders, newBlock))
            {
                // 在这里处理立即碰撞检测后的逻辑
                float elapsedTime = Time.time - creationTime; // 计算从生成到点击的时间
                audioFeedback.PlayAudio(musicBlocks[Index].pitch);

                Vector3 clickTextCenter = musicBlocks[Index].GetFeedbackPosition();
                if (elapsedTime <= 0.25f)
                {
                    UIManager.Instance.ShowFeedbackText("Perfect!", 1.0f,clickTextCenter,musicBlocks[Index].GetRotation(clickTextCenter));
                    UpdateScoreAndCombo("Perfect!");
                }
                else if (elapsedTime > 0.25f)
                {
                    UIManager.Instance.ShowFeedbackText("Good!", 1.0f,clickTextCenter,musicBlocks[Index].GetRotation(clickTextCenter));
                    UpdateScoreAndCombo("Good!");
                }

                Destroy(newBlock);

                yield break; // 退出协程

            }

            yield return null; // 等待下一帧
        }

        UpdateScoreAndCombo("Miss!");
    }

    private IEnumerator Swipe(int startIndex, int endIndex, float lifespan)
    {
        sumBlockCount += 2;
        int blockCount = sumBlockCount;

        // 启动 Hint 协程
        StartCoroutine(Hint(clickhintPrefab, startIndex, hintDuration));
        StartCoroutine(Hint(swipehintPrefab, endIndex, hintDuration));

        // 等待
        yield return new WaitForSeconds(hintDuration);

        GameObject startBlock = InstantiateMusicBlock(clickPrefab, startIndex);
        GameObject endBlock = InstantiateMusicBlock(swipePrefab, endIndex);

        // 记录协程开始时间
        float startBlockTime = Time.time;
        Destroy(startBlock, lifespan);
        Destroy(endBlock, lifespan);

        bool hasHitstartBlock = false;


        while (endBlock != null)
        {

            if (IstentacleCollidingWithBlock(InputManager.Instance.stickColliders, startBlock))
            {
                if (!hasHitstartBlock)
                {
                    hasHitstartBlock = true;
                    startBlockTime = Time.time;
                }
            }
            else if (hasHitstartBlock && IstentacleCollidingWithBlock(InputManager.Instance.stickColliders, endBlock))
            {
                float endBlockTime = Time.time;
                float elapsedTime = endBlockTime - startBlockTime;

                audioFeedback.PlayAudio(musicBlocks[startIndex].pitch);
                Vector3 swipeTextCenter = (musicBlocks[startIndex].GetFeedbackPosition() + musicBlocks[endIndex].GetFeedbackPosition()) / 2;
                UIManager.Instance.ShowFeedbackText("Swipe!", 1.0f,swipeTextCenter,musicBlocks[startIndex].GetRotation(swipeTextCenter));

                UpdateScoreAndCombo("Perfect!");

                Destroy(startBlock);
                Destroy(endBlock);

                yield break;
            }

            yield return null; // 等待下一帧

        }

        UpdateScoreAndCombo("Miss!");
    }

    public List<GameObject> PlaceCubesBetweenPoints(GameObject Prefab, Vector3 pointA, Vector3 pointB, int N)
    {
        float minSize = 0.01f;
        float maxSize = 0.08f;
        List<GameObject> cubes = new List<GameObject>(); // 创建一个用于存储所有方块的列表

        for (int i = 1; i < N - 1; i++)
        {
            float t = i / (float)(N - 1); // 计算插值参数
            Vector3 position = Vector3.Lerp(pointA, pointB, t); // 在A和B之间插值位置
            float size = Mathf.Lerp(minSize, maxSize, t); // 插值大小
            GameObject cube = Instantiate(Prefab, position, Quaternion.identity); // 实例化方块
            cube.transform.localScale = new Vector3(size, size, size); // 设置方块大小
            cubes.Add(cube); // 将实例化的方块添加到列表中
        }

        return cubes; // 返回包含所有方块的列表
    }


    private AudioClip CreateClipWithSilence(AudioClip originalClip, float silenceDuration)
    {
        int originalSamples = originalClip.samples;
        int silenceSamples = (int)(silenceDuration * originalClip.frequency);
        int newSamples = originalSamples + silenceSamples;
        float[] newData = new float[newSamples * originalClip.channels];

        // Fill the new data array with silence first
        for (int i = 0; i < silenceSamples * originalClip.channels; i++)
        {
            newData[i] = 0;
        }

        // Copy the original clip data into the new data array
        float[] originalData = new float[originalSamples * originalClip.channels];
        originalClip.GetData(originalData, 0);
        for (int i = 0; i < originalSamples * originalClip.channels; i++)
        {
            newData[i + silenceSamples * originalClip.channels] = originalData[i];
        }

        // Create a new AudioClip
        AudioClip newClip = AudioClip.Create(originalClip.name + "_with_silence", newSamples, originalClip.channels, originalClip.frequency, false);
        newClip.SetData(newData, 0);

        return newClip;
    }

    private void UpdateScoreAndCombo(string result)
    {
        if (result == "Perfect!")
        {
            ScoreManager.Instance.AddScore(2);
            ScoreManager.Instance.AddCombo(1);
        }
        else if (result == "Good!")
        {
            ScoreManager.Instance.AddScore(1);
            ScoreManager.Instance.AddCombo(1);
        }
        else if (result == "Bubble Perfect!")
        {
            ScoreManager.Instance.AddScore(5);
            ScoreManager.Instance.AddCombo(1);
        }
        else if (result == "Bubble Good!")
        {
            ScoreManager.Instance.AddScore(3);
            ScoreManager.Instance.AddCombo(1);
        }
        else
        {
            ScoreManager.Instance.ResetCombo(0);
        }
    }

    public void restart()
    {
        nextIndex = 0;
    }
}
