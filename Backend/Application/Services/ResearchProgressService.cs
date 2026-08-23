using Application.Interfaces.IServices;
using Domain.User;
using Domain.Workers;

namespace Application.Services;

public sealed record ResearchProgressSnapshot(double ProgressPercentage, DateTime? ExpectedCompletionTime);

public sealed class ResearchProgressService
{
    private const double CompletionTolerance = 0.000001d;
    private readonly IResearchRateCalculator _rateCalculator;

    public ResearchProgressService(IResearchRateCalculator rateCalculator)
    {
        _rateCalculator = rateCalculator;
    }

    public void Initialize(ResearchJob job, WorldPlayer player, double totalWorkSeconds, DateTime startedAtUtc)
    {
        job.TotalWorkSeconds = totalWorkSeconds;
        job.RemainingWorkSeconds = totalWorkSeconds;
        job.LastProgressAt = startedAtUtc;
        job.IsCompleted = false;
        RefreshRateAndSchedule(job, player, startedAtUtc);
    }

    public void AdvanceTo(ResearchJob job, WorldPlayer player, DateTime targetUtc)
    {
        if (job.IsCompleted || targetUtc <= job.LastProgressAt)
        {
            return;
        }

        while (!job.IsCompleted && job.LastProgressAt < targetUtc)
        {
            DateTime intervalEnd = job.ExecutionTime < targetUtc ? job.ExecutionTime : targetUtc;
            if (intervalEnd <= job.LastProgressAt)
            {
                RefreshRateAndSchedule(job, player, job.LastProgressAt);
                if (job.ExecutionTime <= job.LastProgressAt)
                {
                    break;
                }
                continue;
            }

            AdvanceInterval(job, intervalEnd);
            if (job.IsCompleted)
            {
                return;
            }

            if (intervalEnd == job.ExecutionTime)
            {
                RefreshRateAndSchedule(job, player, intervalEnd);
            }
        }
    }

    public void RefreshRateAndSchedule(ResearchJob job, WorldPlayer player, DateTime asOfUtc)
    {
        if (job.IsCompleted)
        {
            return;
        }

        var rate = _rateCalculator.Calculate(player, asOfUtc);
        job.AppliedSpeedMultiplier = rate.SpeedMultiplier;
        job.LastProgressAt = asOfUtc;

        DateTime? completionAt = rate.SpeedMultiplier > 0d
            ? SafeAddSeconds(asOfUtc, job.RemainingWorkSeconds / rate.SpeedMultiplier)
            : null;

        job.ExecutionTime = Earlier(completionAt, rate.NextRateChangeAtUtc) ?? DateTime.MaxValue;
    }

    public ResearchProgressSnapshot Project(ResearchJob source, WorldPlayer player, DateTime asOfUtc)
    {
        var projected = Clone(source);
        AdvanceTo(projected, player, asOfUtc);
        if (!projected.IsCompleted)
        {
            RefreshRateAndSchedule(projected, player, projected.LastProgressAt);
        }

        double progress = projected.TotalWorkSeconds <= 0d
            ? 0d
            : Math.Clamp(
                (projected.TotalWorkSeconds - projected.RemainingWorkSeconds) / projected.TotalWorkSeconds * 100d,
                0d,
                100d);
        DateTime? expectedCompletion = ProjectCompletion(Clone(projected), player);

        return new ResearchProgressSnapshot(progress, expectedCompletion);
    }

    private DateTime? ProjectCompletion(ResearchJob projected, WorldPlayer player)
    {
        if (projected.IsCompleted)
        {
            return projected.LastProgressAt;
        }

        int remainingBoundaries = player.Cities.Sum(city => city.ActiveFocuses.Count * 2) + 2;
        while (!projected.IsCompleted && remainingBoundaries-- > 0)
        {
            if (projected.ExecutionTime == DateTime.MaxValue)
            {
                return null;
            }

            DateTime boundary = projected.ExecutionTime;
            AdvanceTo(projected, player, boundary);
            if (!projected.IsCompleted && projected.ExecutionTime == boundary)
            {
                RefreshRateAndSchedule(projected, player, boundary);
            }
        }

        return projected.IsCompleted ? projected.LastProgressAt : null;
    }

    private static void AdvanceInterval(ResearchJob job, DateTime intervalEnd)
    {
        double elapsedSeconds = (intervalEnd - job.LastProgressAt).TotalSeconds;
        if (job.AppliedSpeedMultiplier > 0d)
        {
            double secondsToCompletion = job.RemainingWorkSeconds / job.AppliedSpeedMultiplier;
            if (secondsToCompletion <= elapsedSeconds + CompletionTolerance)
            {
                job.LastProgressAt = SafeAddSeconds(job.LastProgressAt, Math.Max(0d, secondsToCompletion));
                job.RemainingWorkSeconds = 0d;
                job.ExecutionTime = job.LastProgressAt;
                job.IsCompleted = true;
                return;
            }

            job.RemainingWorkSeconds -= elapsedSeconds * job.AppliedSpeedMultiplier;
        }

        job.LastProgressAt = intervalEnd;
    }

    private static ResearchJob Clone(ResearchJob source) => new()
    {
        Id = source.Id,
        WorldPlayerId = source.WorldPlayerId,
        ResearchId = source.ResearchId,
        TotalWorkSeconds = source.TotalWorkSeconds,
        RemainingWorkSeconds = source.RemainingWorkSeconds,
        LastProgressAt = source.LastProgressAt,
        AppliedSpeedMultiplier = source.AppliedSpeedMultiplier,
        ExecutionTime = source.ExecutionTime,
        IsCompleted = source.IsCompleted
    };

    private static DateTime? Earlier(DateTime? first, DateTime? second)
    {
        if (!first.HasValue) return second;
        if (!second.HasValue) return first;
        return first.Value <= second.Value ? first : second;
    }

    private static DateTime SafeAddSeconds(DateTime timestamp, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds >= (DateTime.MaxValue - timestamp).TotalSeconds)
        {
            return DateTime.MaxValue;
        }

        return timestamp.AddSeconds(Math.Max(0d, seconds));
    }
}
