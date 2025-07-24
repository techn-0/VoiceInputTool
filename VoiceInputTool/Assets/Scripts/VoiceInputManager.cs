using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VoiceInput;

public class VoiceInputManager : MonoBehaviour
{
    /*──────────────────────── 1. Inspector 슬롯 ────────────────────────*/
    [Header("Particle FX")]
    public ParticleSystem fxQ;
    public ParticleSystem fxW;
    public ParticleSystem fxE;
    public ParticleSystem fxR;

    [Header("Grammar (단어 목록만)")]
    [SerializeField] private VoiceGrammar grammar;   // 이벤트 연결 X ― 단어만 사용

    /*──────────────────────── 2. 내부 필드 ─────────────────────────────*/
    private ISttProvider stt;
    private string micDevice;
    private AudioClip micClip;
    private const int RATE = 16000;     // 16 kHz mono
    private const int CHUNK = RATE / 5; // 0.2 s

    // 단어 → 파티클 매핑
    private Dictionary<string, ParticleSystem> map;

    /*──────────────────────── 3. 초기화 ────────────────────────────────*/
    private void Start()
    {
        /* 3‑1. 마이크 장치 확인 */
        micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (micDevice == null)
        {
            Debug.LogError("🛑  No microphone detected."); 
            return;
        }

        /* 3‑2. 마이크 녹음 시작 */
        micClip = Microphone.Start(micDevice, true, 1, RATE);

        /* 3‑3. Vosk STT 초기화 */
        stt = new VoskProvider();
        var modelPath = Path.Combine(Application.streamingAssetsPath, "Models", "ko");
        stt.Init(modelPath, grammar.Words);          // 단어 리스트만 넘김
        stt.OnFinal += OnRecognized;

        /* 3‑4. 단어 ↔ 파티클 매핑 테이블 */
        map = new()
        {
            { "큐",      fxQ },
            { "더블유",   fxW },
            { "이",      fxE },
            { "알",      fxR },
            { "궁",      fxR }   // '알' 대신 '궁'이라고 말할 때도 R 재생
        };

        /* 3‑5. 녹음 데이터 캡처 루프 시작 */
        StartCoroutine(Capture());
    }

    /*──────────────────────── 4. 오디오 캡처 루프 ───────────────────────*/
    private IEnumerator Capture()
    {
        var bufF = new float[CHUNK];
        var bufB = new byte[CHUNK * 2];
        int last = 0;

        while (true)
        {
            int pos = Microphone.GetPosition(micDevice);
            if (pos < 0) { yield return null; continue; }   // device not ready

            int diff = (pos - last + micClip.samples) % micClip.samples;
            if (diff >= CHUNK)
            {
                micClip.GetData(bufF, last);
                for (int i = 0; i < CHUNK; ++i)
                {
                    short s = (short)(Mathf.Clamp(bufF[i], -1, 1) * 32767);
                    bufB[(i << 1)] = (byte)(s & 0xFF);
                    bufB[(i << 1) + 1] = (byte)(s >> 8);
                }
                stt.Feed(bufB);
                last = (last + CHUNK) % micClip.samples;
            }
            yield return null;
        }
    }

    /*──────────────────────── 5. 음성 인식 콜백 ────────────────────────*/
    private void OnRecognized(string raw)
    {
        string w = Clean(raw);
        Debug.Log($"[Voice] \"{w}\"");

        if (map.TryGetValue(w, out var fx))
        {
            fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // 이전 입자 정리
            fx.Play();
            Debug.Log("[Voice] FX played ✔");
        }
        else
        {
            Debug.Log("[Voice] Unmapped word ✖");
        }
    }

    /*──────────────────────── 6. 유틸 ─────────────────────────────────*/
    private static string Clean(string s) =>
        s.Trim().TrimEnd('.', '。', '…', '!', '?').Replace(" ", "");
}
