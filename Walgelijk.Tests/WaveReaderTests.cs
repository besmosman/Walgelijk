﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Walgelijk.OpenTK;

namespace Tests;

[TestClass]
public class WaveReaderTests
{
    [TestMethod]
    public void MonoMicrosoft()
    {
        var file = WaveFileReader.Read("mono_microsoft.wav");
        Assert.AreEqual(1, file.NumChannels);
        Assert.AreEqual(48_000, file.SampleRate);
        Assert.AreEqual(57_374, file.SampleCount);
        Assert.AreEqual(file.SampleCount, file.Data.Length);
    }    
    
    [TestMethod]
    public void StereoMicrosoft()
    {
        var file = WaveFileReader.Read("stereo_microsoft.wav");
        Assert.AreEqual(2, file.NumChannels);
        Assert.AreEqual(44_100, file.SampleRate);
        Assert.AreEqual(456_704, file.SampleCount);
        Assert.AreEqual(file.SampleCount, file.Data.Length);
    }  
    
    [TestMethod]
    public void MonoJunkChunk()
    {
        var file = WaveFileReader.Read("mono_junk_chunk.wav");
        Assert.AreEqual(1, file.NumChannels);
        Assert.AreEqual(44_100, file.SampleRate);
        Assert.AreEqual(67_836, file.SampleCount);
        Assert.AreEqual(file.SampleCount, file.Data.Length);
    }    

    [TestMethod]
    public void BitRateFailure()
    {
        var e = Assert.ThrowsException<Exception>(() =>
        {
            var file = WaveFileReader.Read("24bitwave.wav");
        });
        Assert.IsTrue(e.Message.Contains("24"), "Won't load wave files that aren't 16 bit");
    }
}
