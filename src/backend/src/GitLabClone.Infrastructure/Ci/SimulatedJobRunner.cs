using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Infrastructure.Ci;

/// <summary>
/// Background service that picks up Pending pipelines and simulates running their jobs.
/// Each job "runs" for 1–3 seconds with simulated log output. This is a demo runner —
/// a real implementation would spawn containers or delegate to agents.
/// </summary>
public sealed class SimulatedJobRunner(
    IServiceScopeFactory scopeFactory,
    ILogger<SimulatedJobRunner> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SimulatedJobRunner started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingPipelinesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing pipelines");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task ProcessPendingPipelinesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var pendingPipelines = await db.Pipelines
            .Include(p => p.Jobs)
            .Where(p => p.Status == PipelineStatus.Pending)
            .OrderBy(p => p.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        foreach (var pipeline in pendingPipelines)
        {
            logger.LogInformation("Running pipeline {PipelineId} for ref {Ref}", pipeline.Id, pipeline.Ref);

            pipeline.Status = PipelineStatus.Running;
            pipeline.StartedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            var allPassed = true;

            // Group jobs by stage and run stages sequentially
            var jobsByStage = pipeline.Jobs.GroupBy(j => j.Stage).OrderBy(g => g.Key);

            foreach (var stageGroup in jobsByStage)
            {
                foreach (var job in stageGroup)
                {
                    job.Status = JobStatus.Running;
                    job.StartedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(ct);

                    // Simulate job execution
                    await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(500, 2000)), ct);

                    // 90% chance of success for simulation
                    var success = Random.Shared.NextDouble() < 0.9;
                    job.Status = success ? JobStatus.Success : JobStatus.Failed;
                    job.FinishedAt = DateTimeOffset.UtcNow;
                    job.Log = GenerateJobLog(job.Name, job.Stage, success);

                    if (!success) allPassed = false;
                    await db.SaveChangesAsync(ct);
                }

                // If any job in stage failed, skip remaining stages
                if (!allPassed)
                {
                    foreach (var remaining in pipeline.Jobs.Where(j => j.Status == JobStatus.Pending))
                    {
                        remaining.Status = JobStatus.Skipped;
                        remaining.Log = "Skipped due to previous stage failure.";
                    }
                    break;
                }
            }

            pipeline.Status = allPassed ? PipelineStatus.Success : PipelineStatus.Failed;
            pipeline.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Pipeline {PipelineId} finished with status {Status}", pipeline.Id, pipeline.Status);
        }
    }

    private static string GenerateJobLog(string jobName, string stage, bool success)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        return $"""
            [{timestamp}] Starting job '{jobName}' in stage '{stage}'...
            [{timestamp}] Pulling environment...
            [{timestamp}] Executing script commands...
            [{timestamp}] $ echo "Running {jobName}"
            Running {jobName}
            [{timestamp}] Job {(success ? "succeeded" : "FAILED")} with exit code {(success ? 0 : 1)}.
            """;
    }
}
