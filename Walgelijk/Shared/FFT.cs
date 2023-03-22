﻿using System;
using System.Buffers;
using System.Numerics;

namespace Walgelijk;

public class AudioVisualiser
{
    public readonly Sound Sound;

    public readonly int BufferSize;
    public readonly int BinCount;
    public readonly int FftSize;
    public float MinFreq = 20;
    public float MaxFreq = 16000;
    public int InputBlur = 0;
    public int OutputBlur = 2;

    private readonly float[] samples;
    private readonly float[] fft;
    private readonly float[] sampleAccumulator;

    private readonly Bin[] bins;
    private readonly float[] visualiser;

    private int accumulationCursor = 0;

    private class Bin
    {
        public float Value = 0;
        public Memory<float> Section;
        public int Start;

        public Bin(Memory<float> section)
        {
            Section = section;
        }

        public float CalculateValue()
        {
            float v = 0;
            var span = Section.Span;
            for (int i = 0; i < Section.Length; i++)
                v = MathF.Max(span[i], v);
            return /*10f * MathF.Log10(v)*/v;
        }
    }

    public AudioVisualiser(Sound sound, int fftSize = 4096, int bufferSize = 1024, int binCount = 128)
    {
        BufferSize = bufferSize;
        BinCount = binCount;
        Sound = sound;
        FftSize = fftSize;

        sampleAccumulator = new float[FftSize];
        samples = new float[BufferSize];
        fft = new float[FftSize];

        visualiser = new float[BinCount];
        bins = new Bin[BinCount];
        for (int i = 0; i < bins.Length; i++)
            bins[i] = new Bin(Memory<float>.Empty);
    }

    public int FrequencyToFftIndex(float freq) => (int)((Sound.Data.SampleRate / (float)FftSize) / freq);
    public int FftIndexToBinIndex(int i, float gamma = 2) => (int)(MathF.Pow(i / (float)FftSize / 2, 1f / gamma) * BinCount);

    private void UpdateFft(AudioRenderer audio)
    {
        int readSampleCount = audio.GetCurrentSamples(Sound, samples);

        if (accumulationCursor < FftSize) // if the amount of accumulated samples have not yet reached the end 
        {
            if (Sound.Data.ChannelCount == 2)
            {
                //stereo! channels are interleaved! take left
                for (int i = 0; i < readSampleCount; i += 2)
                    sampleAccumulator[accumulationCursor++] = samples[i];
            }
            else
            {
                //mono! take everything as-is
                samples.CopyTo(sampleAccumulator, accumulationCursor);
                accumulationCursor += readSampleCount;
            }
        }
        else
        {
            for (int i = 0; i < InputBlur; i++)
                AudioAnalysis.BlurSignal(sampleAccumulator);

            AudioAnalysis.Fft(sampleAccumulator, fft);

            //shift sampleAccumulator to the left halfway to let the windows overlap
            sampleAccumulator.AsSpan(sampleAccumulator.Length - BufferSize / 2, BufferSize / 2).CopyTo(sampleAccumulator);
            accumulationCursor = BufferSize / 2;
        }
    }

    private void PopulateBins(float dt)
    {
        foreach (var item in bins)
        {
            item.Section = Memory<float>.Empty;
            item.Start = -1;
        }

        for (int i = 0; i < FftSize / 2; i++)
        {
            var bin = bins[FftIndexToBinIndex(i)];
            if (bin.Section.Length == 0 || bin.Start == -1) // empty bin
            {
                bin.Section = fft.AsMemory(i, 1);
                bin.Start = i;
            }
            else
            {
                bin.Section = fft.AsMemory(bin.Start, i - bin.Start);
            }
        }

        foreach (var item in bins)
        {
            item.Value = Utilities.SmoothApproach(item.Value, item.Section.Length > 0 ? (item.CalculateValue()) : 0, 64, dt);
        }

        //for (int i = 0; i < BinCount; i++)
        //{
        //    float min = Utilities.MapRange(0, BinCount - 1, MinFreq, MaxFreq, i);
        //    float max = Utilities.MapRange(0, BinCount - 1, MinFreq, MaxFreq, i + 1);

        //    FftIndexToBinIndex
        //}
    }

    public void Update(AudioRenderer audio, float dt)
    {
        UpdateFft(audio);
        PopulateBins(dt);

        for (int i = 0; i < visualiser.Length; i++)
        {
            visualiser[i] = Utilities.SmoothApproach(visualiser[i], bins[i].Value, 64, dt);
        }

        for (int i = 0; i < OutputBlur; i++)
            AudioAnalysis.BlurSignal(visualiser);
    }

    public ReadOnlySpan<float> GetVisualiserData() => visualiser;
}

public struct AudioAnalysis
{
    public static void BlurSignal(Span<float> values)
    {
        var s = ArrayPool<float>.Shared.Rent(values.Length);
        var scrap = s.AsSpan(0, values.Length);
        values.CopyTo(scrap);
        for (int i = 1; i < values.Length - 1; i++)
            values[i] = (scrap[i - 1] + scrap[i + 1] + scrap[i]) / 3f;
        ArrayPool<float>.Shared.Return(s);
    }

    public static float Rectangular(float v, int n, int N)
    {
        return v;
    }

    public static float Blackman(float v, int n, int N)
    {
        return v * (0.42f - 0.5f * MathF.Cos(2 * MathF.PI * n / (N - 1)) + 0.08f * MathF.Cos(4 * MathF.PI * n / (N - 1)));
    }

    public static float Hamming(float v, int n, int N)
    {
        return v * (0.54f - 0.46f * MathF.Cos(2 * MathF.PI * n / (N - 1)));
    }

    public static Span<float> Fft(ReadOnlySpan<float> input, Span<float> result)
    {
        if (input.Length != result.Length)
            throw new Exception("input length and result length should be equal");
        bool isPowerOfTwo = (result.Length > 0) && ((result.Length & (result.Length - 1)) == 0);

        if (!isPowerOfTwo)
            throw new Exception("Length should be a power of two");

        int n = input.Length;

        var samples = ArrayPool<Complex>.Shared.Rent(n);
        var fft = ArrayPool<Complex>.Shared.Rent(n);

        for (int i = 0; i < n; i++)
            samples[i] = new Complex(Blackman(input[i], i, n), 0);

        Fft(samples, fft);

        for (int i = 0; i < n; i++)
        {
            var m = fft[i];
            var v = (float)((m.Real * m.Real + m.Imaginary * m.Imaginary));
            if (float.IsInfinity(v) || v < 0)
                v = 0;
            result[i] = v;
        }

        ArrayPool<Complex>.Shared.Return(samples, true);
        ArrayPool<Complex>.Shared.Return(fft, true);

        return result[0..(n / 2)];
    }

    public static void Fft(ReadOnlySpan<Complex> arr, Span<Complex> output)
    {
        var length = arr.Length;
        var hLength = length >> 1; //length is power of two dus length / 2 is hetzelfde als bitshift
        if (length == 1)
        {
            arr.CopyTo(output);
            return;
        }

        var eca = ArrayPool<Complex>.Shared.Rent(hLength);
        var oda = ArrayPool<Complex>.Shared.Rent(hLength);
        var ra = ArrayPool<Complex>.Shared.Rent(length);

        var evenCoefficients = eca.AsSpan(0, hLength);
        var oddCoefficients = oda.AsSpan(0, hLength);
        var roots = ra.AsSpan(0, length);

        for (var i = 0; i < length; i++)
        {
            var th = Math.Tau * (i / (double)length);
            roots[i] = new Complex(Math.Cos(th), Math.Sin(th));
        }
        for (var i = 0; i < hLength; i++)
        {
            evenCoefficients[i] = arr[i * 2];
            oddCoefficients[i] = arr[i * 2 + 1];
        }

        var eoca = ArrayPool<Complex>.Shared.Rent(hLength);
        var ooca = ArrayPool<Complex>.Shared.Rent(hLength);
        var evenOrderCoefficientTransform = eoca.AsSpan(0, hLength);
        var oddOrderCoefficientTransform = ooca.AsSpan(0, hLength);

        Fft(evenCoefficients, evenOrderCoefficientTransform);
        Fft(oddCoefficients, oddOrderCoefficientTransform);

        for (var i = 0; i < hLength; i++)
        {
            output[i] = evenOrderCoefficientTransform[i] + roots[i] * oddOrderCoefficientTransform[i];
            output[i + hLength] = evenOrderCoefficientTransform[i] - roots[i] * oddOrderCoefficientTransform[i];
        }

        ArrayPool<Complex>.Shared.Return(eca);
        ArrayPool<Complex>.Shared.Return(oda);
        ArrayPool<Complex>.Shared.Return(ra);
        ArrayPool<Complex>.Shared.Return(eoca);
        ArrayPool<Complex>.Shared.Return(ooca);
    }
}