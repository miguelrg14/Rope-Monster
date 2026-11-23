using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class SavingSystem
{
    private const string DATA_PATH = "/ropeMonster";

    public static void Save(GameData data)
    {
        //Nothing to save yet

        if (data == null)
            return;

        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream fs = new FileStream(GetDataPath(), FileMode.Create))
        {
            formatter.Serialize(fs, data);
        }
    }

    public static GameData Load()
    {
        //No file found

        if (!File.Exists(GetDataPath()))
            return null;

        //Deserialize and return data (an empty or corrupted file starts a new game)

        try
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(GetDataPath(), FileMode.Open))
            {
                return formatter.Deserialize(fs) as GameData;
            }
        }
        catch (System.Exception e) when (e is SerializationException || e is IOException)
        {
            Debug.LogWarning($"Save file could not be read, starting a new game: {e.Message}");
            return null;
        }
    }

    private static string GetDataPath()
    {
        return Application.persistentDataPath + DATA_PATH;
    }
}