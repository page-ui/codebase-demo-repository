using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Page.Ui.Backend.Tests.TestSupport;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.SvelteRender.Models;
using Page.Ui.SvelteRender.Services;
using StackExchange.Redis;

namespace Page.Ui.Backend.Tests.SvelteRender;

public class RenderRunMetadataStoreTests
{
    [Fact]
    public async Task GetByUserIdAsync_LoadsCachedRunsWithSingleMultiGet()
    {
        using var dbContext = TestDbFactory.CreateContext();
        var (redis, database) = TestRedisFactory.Create();
        var first = new RenderRun
        {
            RunId = "run-1",
            PublicRunToken = "token-1",
            UserId = "user-1",
            RelativeRunPath = "user/chat/run-1"
        };
        var second = new RenderRun
        {
            RunId = "run-2",
            PublicRunToken = "token-2",
            UserId = "user-1",
            RelativeRunPath = "user/chat/run-2"
        };

        database.Setup(x => x.SortedSetRangeByRankAsync(
                "render:user:user-1:runs",
                0,
                99,
                Order.Descending,
                CommandFlags.None))
            .ReturnsAsync(new RedisValue[] { "run-2", "run-1" });
        database.Setup(x => x.StringGetAsync(
                It.Is<RedisKey[]>(keys => keys.Length == 2),
                CommandFlags.None))
            .ReturnsAsync((RedisKey[] keys, CommandFlags _) => keys
                .Select(key => key.ToString() == "render:run:run-2" ? JsonSerializer.Serialize(second) : JsonSerializer.Serialize(first))
                .Select(value => (RedisValue)value)
                .ToArray());

        var store = new RenderRunMetadataStore(dbContext, redis.Object, NullLogger<RenderRunMetadataStore>.Instance);

        var runs = await store.GetByUserIdAsync("user-1", 1, 100, CancellationToken.None);

        Assert.Equal(new[] { "run-2", "run-1" }, runs.Select(run => run.RunId));
        database.Verify(x => x.StringGetAsync(It.IsAny<RedisKey[]>(), CommandFlags.None), Times.Once);
        database.Verify(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None), Times.Never);
    }

    [Fact]
    public async Task GetByPublicRunTokenAsync_ResolvesLegacyRunIdOnlyTokenAfterCacheMiss()
    {
        using var dbContext = TestDbFactory.CreateContext();
        var runId = "11111111-1111-1111-1111-111111111111";
        dbContext.RenderRuns.Add(new RenderRun
        {
            RunId = runId,
            PublicRunToken = "run_pub_context_bound",
            UserId = "user-1",
            RelativeRunPath = "user/chat/version",
            Status = RenderRunStatus.Succeeded
        });
        await dbContext.SaveChangesAsync();

        var (redis, database) = TestRedisFactory.Create();
        database.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);
        var store = new RenderRunMetadataStore(dbContext, redis.Object, NullLogger<RenderRunMetadataStore>.Instance);

        var run = await store.GetByPublicRunTokenAsync(RenderRunPublicToken.FromRunId(runId), CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(runId, run!.RunId);
    }

    [Fact]
    public void RenderOptions_DefaultsDisableTimeBasedRunCleanup()
    {
        var options = new RenderOptions();

        Assert.False(options.EnableRunCacheCleanup);
        Assert.Equal(0, options.RunCacheMaxAgeHours);
    }
}
