using System;
using System.Collections.Generic;
using System.IO;
using BattleTech;

namespace CustomSalvage;

public abstract class CustomResource
{
    public string ID { get; set; }
}

public static class CustomLoader<T>
    where T : CustomResource, new()
{
    public static List<T> Load(Dictionary<string, VersionManifestEntry> manifest)
    {
        var result = new List<T>();

        foreach (var item in manifest.Values)
        {
            string json = "";
            using (var reader = new StreamReader(item.FilePath))
            {
                json = reader.ReadToEnd();
            }
            try
            {
                var obj = fastJSON.JSON.ToObject<List<T>>(json);
                result.AddRange(obj);
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log($"Error reading {item.FilePath}", e);
            }
        }
        return result;


    }
}