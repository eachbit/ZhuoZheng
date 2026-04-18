using System;
using System.IO;
using System.Text;

public static class WavUtility
{
    public static byte[] FromAudioClip(float[] samples, int channels, int sampleRate)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int sampleCount = samples.Length;
            int byteCount = sampleCount * 2;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + byteCount);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(byteCount);

            for (int i = 0; i < sampleCount; i++)
            {
                float sample = Math.Max(-1f, Math.Min(1f, samples[i]));
                short intSample = (short)(sample * short.MaxValue);
                writer.Write(intSample);
            }

            writer.Flush();
            return stream.ToArray();
        }
    }
}
