using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoiceInput
{
    public class VoskProvider : ISttProvider
    {
        const string DLL = "libvosk";

        [DllImport(DLL)] static extern int vosk_set_log_level(int lvl);
        [DllImport(DLL)] static extern IntPtr vosk_model_new(string path);
        [DllImport(DLL)] static extern IntPtr vosk_recognizer_new(
            IntPtr model, float sampleRate);
        [DllImport(DLL)] static extern int vosk_recognizer_accept_waveform(
            IntPtr rec, byte[] data, int len);
        [DllImport(DLL)] static extern IntPtr vosk_recognizer_final_result(
            IntPtr rec);

        IntPtr _model, _rec;
        public event Action<string> OnFinal;

        public void Init(string modelPath, string[] grammar)
        {
            vosk_set_log_level(0);
            _model = vosk_model_new(modelPath);
            _rec   = vosk_recognizer_new(_model, 16000);
            // grammar는 여기서는 생략 (ko-small은 키워드짜리여도 충분)
        }

        public void Feed(byte[] pcm16)
        {
            if (vosk_recognizer_accept_waveform(_rec, pcm16, pcm16.Length) != 0)
            {
                var resPtr = vosk_recognizer_final_result(_rec);
                var json   = Marshal.PtrToStringAnsi(resPtr);
                var text   = Extract(json);   // 아래 작은 함수
                if (!string.IsNullOrEmpty(text)) OnFinal?.Invoke(text);
            }
        }
        string Extract(string json)
        {
            // 매우 단순 파서: "text" : "큐"
            int idx = json.IndexOf("\"text\""); if (idx < 0) return null;
            int q1 = json.IndexOf('"', idx + 7) + 1;
            int q2 = json.IndexOf('"', q1);
            return json.Substring(q1, q2 - q1).Trim();
        }
    }
}
