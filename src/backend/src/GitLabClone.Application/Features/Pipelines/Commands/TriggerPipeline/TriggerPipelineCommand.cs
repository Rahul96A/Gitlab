using FluentValidation;
using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Pipelines.Dtos;
using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Enums;
using GitLabClone.Domain.Events;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Pipelines.Commands.TriggerPipeline;

public sealed record TriggerPipelineCommand(
    string Slug,
    string Ref
) : IRequest<PipelineDto>;

public sealed class TriggerPipelineCommandValidator : AbstractValidator<TriggerPipelineCommand>
{
    public TriggerPipelineCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Ref).NotEmpty().MaximumLength(256);
    }
}

public sealed class TriggerPipelineCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAppDbContext db,
    IGitService gitService,
    ICiYamlParser ciParser
) : IRequestHandler<TriggerPipelineCommand, PipelineDto>
{
    public async Task<PipelineDto> Handle(TriggerPipelineCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Must be authenticated.");

        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        // Try to read .gitlab-ci.yml from the repo
        var ciFile = await gitService.GetFileContentAsync(
            project.RepositoryPath, request.Ref, ".gitlab-ci.yml", cancellationToken);

        var yamlContent = ciFile?.Content ?? """
            stages:
              - build
              - test
            build-job:
              stage: build
              script:
                - echo "Building..."
            test-job:
              stage: test
              script:
                - echo "Testing..."
            """;

        // Get latest commit SHA
        var commits = await gitService.GetCommitLogAsync(project.RepositoryPath, request.Ref, 1, cancellationToken);
        var commitSha = commits.FirstOrDefault()?.Sha ?? "0000000000000000000000000000000000000000";

        // Parse YAML to create jobs
        var config = ciParser.Parse(yamlContent);

        var pipeline = new Pipeline
        {
            Ref = request.Ref,
            CommitSha = commitSha,
            Status = PipelineStatus.Pending,
            YamlContent = yamlContent,
            ProjectId = project.Id,
            TriggeredById = userId
        };

        foreach (var jobConfig in config.Jobs)
        {
            pipeline.Jobs.Add(new PipelineJob
            {
                Name = jobConfig.Name,
                Stage = jobConfig.Stage,
                Status = JobStatus.Pending
            });
        }

        pipeline.AddDomainEvent(new PipelineTriggeredEvent(pipeline.Id, project.Id, request.Ref));

        await db.Pipelines.AddAsync(pipeline, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await db.Users.FindAsync([userId], cancellationToken);

        return new PipelineDto(
            pipeline.Id, pipeline.Ref, pipeline.CommitSha,
            pipeline.Status.ToString(),
            pipeline.StartedAt, pipeline.FinishedAt,
            user?.Username ?? "unknown",
            pipeline.Jobs.Count, pipeline.CreatedAt
        );
    }
}
