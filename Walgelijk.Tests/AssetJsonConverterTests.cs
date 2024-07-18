﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;

namespace Tests.AssetManager;

[TestClass]
public class AssetJsonConverterTests
{
    [TestMethod]
    public void AssetId()
    {
        AssetId[] p = [
            new("data/test.txt"),
            new("textures/lol.png"),
            new("textures/ui/star.qoi"),
        ];

        var json = JsonConvert.SerializeObject(p);
        var returned = JsonConvert.DeserializeObject<AssetId[]>(json);

        Assert.IsTrue(p.SequenceEqual(returned));
    }   
    
    [TestMethod]
    public void StringAssetId()
    {
        var des = JsonConvert.DeserializeObject<AssetId[]>(@"[""data/test.txt"", ""textures/lol.png"", ""textures/ui/star.qoi""]");

        Assert.AreEqual(new("data/test.txt"), des[0]);
        Assert.AreEqual(new("textures/lol.png"), des[1]);
        Assert.AreEqual(new("textures/ui/star.qoi"), des[2]);
    }   
    
    [TestMethod]
    public void GlobalAssetId()
    {
        GlobalAssetId[] p = [
            new("test:data/test.txt"),
            new("gaming:textures/lol.png"),
            new("assets:textures/ui/star.qoi"),
        ];

        var json = JsonConvert.SerializeObject(p);
        var returned = JsonConvert.DeserializeObject<GlobalAssetId[]>(json);

        Assert.IsTrue(p.SequenceEqual(returned));
    }   
    
    [TestMethod]
    public void StringGlobalAssetId()
    {
        var des = JsonConvert.DeserializeObject<GlobalAssetId[]>(@"[""test:data/test.txt"", ""gaming:textures/lol.png"", ""assets:textures/ui/star.qoi""]");

        Assert.AreEqual(new("test:data/test.txt"), des[0]);
        Assert.AreEqual(new("gaming:textures/lol.png"), des[1]);
        Assert.AreEqual(new("assets:textures/ui/star.qoi"), des[2]);
    }    
    
    [TestMethod]
    public void AssetRef()
    {
        AssetRef<string> a0 = new("test:data/test.txt");
        AssetRef<Texture> a1 = new("gaming:textures/lol.png");
        AssetRef<Texture> a2 = new("assets:textures/ui/star.qoi");

        check(a0);
        check(a1);
        check(a2);
        
        void check<T>(AssetRef<T> expected)
        {
            var j = JsonConvert.SerializeObject(expected);
            var des = JsonConvert.DeserializeObject<AssetRef<T>>(j);
            Assert.AreEqual(expected, des);
        }
    }  
    
    [TestMethod]
    public void NumericalIDs()
    {
        var des = JsonConvert.DeserializeObject<AssetRef<AudioData>[]>(@$"[""-745702066:-1160611600"", ""{1673332487:X2}:{-401014186:X2}"", ""1023053047:{-465793011:X2}""]");

        Assert.AreEqual(new("test:data/test.txt"), des[0]);
        Assert.AreEqual(new("gaming:textures/lol.png"), des[1]);
        Assert.AreEqual(new("assets:textures/ui/star.qoi"), des[2]);
    }   
    
    [TestMethod]
    public void StringAssetRef()
    {
        var des = JsonConvert.DeserializeObject<AssetRef<AudioData>[]>(@"[""test:data/test.txt"", ""gaming:textures/lol.png"", ""assets:textures/ui/star.qoi""]");

        Assert.AreEqual(new("test:data/test.txt"), des[0]);
        Assert.AreEqual(new("gaming:textures/lol.png"), des[1]);
        Assert.AreEqual(new("assets:textures/ui/star.qoi"), des[2]);
    }    
}
