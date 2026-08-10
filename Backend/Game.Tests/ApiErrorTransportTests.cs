using System.Text.Json;
using Game.Contracts;
using Game.Middleware;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Game.Tests;

public class ApiErrorTransportTests
{
    [Fact]
    public async Task ResultFilter_ReplacesServerErrorPayloadWithGenericApiError()
    {
        var result = await ExecuteFilterAsync(new ObjectResult("database password leaked")
        {
            StatusCode = StatusCodes.Status500InternalServerError
        });

        var objectResult = Assert.IsType<ObjectResult>(result);
        var error = Assert.IsType<ApiError>(objectResult.Value);
        Assert.Equal("server.error", error.Code);
        Assert.Equal("En intern serverfejl opstod.", error.Message);
        Assert.DoesNotContain("password", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResultFilter_PreservesExplicitApiError()
    {
        var expected = new ApiError("deployment.invalid_state", "Handlingen kunne ikke udføres.");

        var result = await ExecuteFilterAsync(new BadRequestObjectResult(expected));

        var objectResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task ExceptionMiddleware_DoesNotExposeUnexpectedExceptionMessage()
    {
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidDataException("database password leaked"),
            NullLogger<ApiExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ApiError>(context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("server.error", error.Code);
        Assert.DoesNotContain("password", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExceptionMiddleware_UsesStableMessageForKnownException()
    {
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("internal deployment invariant"),
            NullLogger<ApiExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ApiError>(context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("resource.conflict", error.Code);
        Assert.DoesNotContain("invariant", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExceptionMiddleware_LogsConcurrencyEntityTypeKeyAndState()
    {
        Guid assignmentId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var databaseContext = new GameContext(options);
        var assignment = new DailyObjectiveAssignment { Id = assignmentId };
        databaseContext.Attach(assignment);
        databaseContext.Entry(assignment).State = EntityState.Modified;
#pragma warning disable EF1001
        var exception = new DbUpdateConcurrencyException(
            "daily conflict",
            null,
            [databaseContext.Entry(assignment).GetInfrastructure()]);
#pragma warning restore EF1001
        var logger = new RecordingLogger<ApiExceptionMiddleware>();
        var middleware = new ApiExceptionMiddleware(_ => throw exception, logger);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        string log = Assert.Single(logger.Messages);
        Assert.Contains(nameof(DailyObjectiveAssignment), log);
        Assert.Contains($"Id={assignmentId}", log);
        Assert.Contains("State=Modified", log);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    private static async Task<IActionResult> ExecuteFilterAsync(IActionResult result)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var executingContext = new ResultExecutingContext(actionContext, filters, result, new object());
        var filter = new ApiErrorResultFilter();

        await filter.OnResultExecutionAsync(executingContext, () => Task.FromResult(
            new ResultExecutedContext(actionContext, filters, executingContext.Result, new object())));

        return executingContext.Result;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
