using GitLabClone.Application.Common.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitLabClone.Infrastructure.Ci;

public sealed class CiYamlParser : ICiYamlParser
{
    private static readonly HashSet<string> ReservedKeys =
        ["stages", "variables", "image", "services", "before_script", "after_script", "cache", "default", "include", "workflow"];

    public CiPipelineConfig Parse(string yamlContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var root = deserializer.Deserialize<Dictionary<string, object>>(yamlContent)
            ?? throw new InvalidOperationException("Invalid CI YAML: empty document.");

        // Parse stages
        var stages = new List<CiStageConfig>();
        if (root.TryGetValue("stages", out var stagesObj) && stagesObj is IList<object> stageList)
        {
            for (var i = 0; i < stageList.Count; i++)
                stages.Add(new CiStageConfig(stageList[i]?.ToString() ?? $"stage-{i}", i));
        }
        else
        {
            stages.Add(new CiStageConfig("build", 0));
            stages.Add(new CiStageConfig("test", 1));
            stages.Add(new CiStageConfig("deploy", 2));
        }

        // Parse jobs (any top-level key that's not reserved)
        var jobs = new List<CiJobConfig>();
        foreach (var (key, value) in root)
        {
            if (ReservedKeys.Contains(key) || key.StartsWith('.'))
                continue;

            if (value is not Dictionary<object, object> jobMap)
                continue;

            var stage = jobMap.TryGetValue("stage", out var s) ? s?.ToString() ?? "test" : "test";

            var script = new List<string>();
            if (jobMap.TryGetValue("script", out var scriptObj))
            {
                if (scriptObj is IList<object> scriptList)
                    script.AddRange(scriptList.Select(x => x?.ToString() ?? ""));
                else if (scriptObj is string scriptStr)
                    script.Add(scriptStr);
            }

            List<string>? only = null;
            if (jobMap.TryGetValue("only", out var onlyObj) && onlyObj is IList<object> onlyList)
                only = onlyList.Select(x => x?.ToString() ?? "").ToList();

            List<string>? artifacts = null;
            if (jobMap.TryGetValue("artifacts", out var artObj) && artObj is Dictionary<object, object> artMap)
            {
                if (artMap.TryGetValue("paths", out var pathsObj) && pathsObj is IList<object> pathsList)
                    artifacts = pathsList.Select(x => x?.ToString() ?? "").ToList();
            }

            jobs.Add(new CiJobConfig(key, stage, script, only, artifacts));
        }

        // If no jobs found, create a default placeholder
        if (jobs.Count == 0)
        {
            jobs.Add(new CiJobConfig("default-job", "build", ["echo \"No jobs defined\""]));
        }

        return new CiPipelineConfig(stages, jobs);
    }
}
