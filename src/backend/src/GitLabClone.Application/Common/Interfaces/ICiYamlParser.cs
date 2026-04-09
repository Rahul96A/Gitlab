namespace GitLabClone.Application.Common.Interfaces;

public interface ICiYamlParser
{
    CiPipelineConfig Parse(string yamlContent);
}

public record CiPipelineConfig(
    IReadOnlyList<CiStageConfig> Stages,
    IReadOnlyList<CiJobConfig> Jobs
);

public record CiStageConfig(string Name, int Order);

public record CiJobConfig(
    string Name,
    string Stage,
    IReadOnlyList<string> Script,
    IReadOnlyList<string>? Only = null,
    IReadOnlyList<string>? Artifacts = null
);
