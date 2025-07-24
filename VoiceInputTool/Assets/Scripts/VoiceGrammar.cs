using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Voice/Grammar")]
public class VoiceGrammar : ScriptableObject
{
    [System.Serializable]
    public struct Pair
    {
        public string word;             // 음성 단어
        public UnityEvent onRecognized; // 실행 이벤트
    }
    public Pair[] pairs;

    public string[] Words => pairs.Select(p => p.word).ToArray();

    public bool InvokeIfMatch(string w)
    {
        foreach (var p in pairs)
        {
            if (p.word == w)
            {
                p.onRecognized.Invoke();
                return true;          // 한 단어만 매칭되면 바로 true
            }
        }
        return false;                 // 끝까지 못 찾으면 false
    }
}
