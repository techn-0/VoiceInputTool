namespace VoiceInput
{
    public interface ISttProvider
    {
        void Init(string modelPath, string[] grammar);
        void Feed(byte[] pcm16);          // 16‑bit little‑endian
        event System.Action<string> OnFinal;
    }
}
