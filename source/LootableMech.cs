using CustomComponents;
using HBS.Collections;
using Newtonsoft.Json;

namespace CustomSalvage;

[CustomComponent("LootableMech")]

public class LootableMech : SimpleCustomChassis
{
    public string ReplaceID { get; set; }
}

public class SearchTags
{
    public string[] ShouldHaveTags { get; set; } = new string[0];
    public string[] ShouldNotHaveTags { get; set; } = new string[0];
    public string[] ExcludeSelfTags { get; set; } = new string[0];
    [JsonIgnore]
    public TagSet f_shouldHaveTags = null;
    [JsonIgnore]
    public TagSet shouldHaveTags
    {
        get
        {
            if(f_shouldHaveTags == null)
            {
                f_shouldHaveTags = new TagSet();
                f_shouldHaveTags.AddRange(ShouldHaveTags);
            }
            return f_shouldHaveTags;
        }
    }
    [JsonIgnore]
    public TagSet f_shouldNotHaveTags = null;
    [JsonIgnore]
    public TagSet shouldNotHaveTags
    {
        get
        {
            if (f_shouldNotHaveTags == null)
            {
                f_shouldNotHaveTags = new TagSet();
                f_shouldNotHaveTags.AddRange(ShouldNotHaveTags);
            }
            return f_shouldNotHaveTags;
        }
    }
    [JsonIgnore]
    public TagSet f_excludeSelfTags = null;
    [JsonIgnore]
    public TagSet excludeSelfTags
    {
        get
        {
            if (f_excludeSelfTags == null)
            {
                f_excludeSelfTags = new TagSet();
                f_excludeSelfTags.AddRange(ExcludeSelfTags);
            }
            return f_excludeSelfTags;
        }
    }
}
[CustomComponent("LootableUniqueMech")]
public class LootableUniqueMech : SimpleCustomChassis
{
    public string ReplaceID { get; set; }
    public SearchTags randomSearchTags { get; set; } = new SearchTags();
}
