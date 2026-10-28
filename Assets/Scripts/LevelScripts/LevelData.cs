using System;
using System.Collections.Generic;
using Scenes;
using UnityEngine.SceneManagement;

public enum LevelType {
    Normal,
    Elite,
    Secret,
    Custom,
    Other
}

public abstract class LevelData : Enum<LevelData> {
    public abstract string PlainName { get; }
    public abstract string SceneName { get; }
    public virtual string SoundTrack { get; } = "";

    public abstract LevelType Type { get; }
    public abstract LevelData NextLevel { get; }
    public abstract LevelData PrevLevel { get; }

    // Helpers
    public static LevelData Get<T>() where T : LevelData => ParseFromType(typeof(T));
    public static LevelData Parse(Type type) => ParseFromType(type);
    public static LevelData FromScene(Scene scene) {
        LevelData value = SceneToLevelMap.GetValueOrDefault(scene.name);
        if (value == null)
            throw new KeyNotFoundException($"No LevelData found for scene, please add a LevelData for scene: {scene.name}");
        return value;
    }

    static LevelData() {
        InitializeSceneToLevelMap();
    }

    private static readonly Dictionary<string, LevelData> SceneToLevelMap = new();

    private static void InitializeSceneToLevelMap() {
        foreach (LevelData item in Values) {
            SceneToLevelMap[item.SceneName] = item;
        }
    }
}

public class Title : LevelData {
    public override string PlainName => "Title";
    public override string SceneName => "1_Title";
    public override LevelType Type => LevelType.Other;
    public override LevelData NextLevel => Get<Tutorial>();
    public override LevelData PrevLevel => Get<Title>();
}

public class Tutorial : LevelData {
    public override string PlainName => "Tutorial";
    public override string SceneName => "4_Tutorial";
    public override LevelType Type => LevelType.Other;
    public override LevelData NextLevel => Get<City>();
    public override LevelData PrevLevel => Get<Title>();
}

public class City : LevelData {
    public override string PlainName => "City";
    public override string SceneName => "5_City";
    public override LevelType Type => LevelType.Other;
    public override LevelData NextLevel => Get<Level_Credits>();
    public override LevelData PrevLevel => Get<Tutorial>();
}

public class MainScene : LevelData {
    public override string PlainName => "Main Scene";
    public override string SceneName => "MainScene";
    public override LevelType Type => LevelType.Other;
    public override LevelData NextLevel => Get<Title>();
    public override LevelData PrevLevel => Get<Title>();
}

public class Level_Credits : LevelData {
    public override string PlainName => "Credits";
    public override string SceneName => "Credits";
    public override LevelType Type => LevelType.Other;
    public override LevelData NextLevel => Get<Title>();
    public override LevelData PrevLevel => Get<City>();
}

public class TestScene : LevelData {
    public override string PlainName => "Test Scene";
    public override string SceneName => "MainScene";
    public override LevelType Type => LevelType.Other;
    public override LevelData NextLevel => Get<Title>();
    public override LevelData PrevLevel => Get<Title>();
}
