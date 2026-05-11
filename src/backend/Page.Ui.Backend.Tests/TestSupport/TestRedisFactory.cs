using Moq;
using StackExchange.Redis;

namespace Page.Ui.Backend.Tests.TestSupport;

internal static class TestRedisFactory
{
    public static (Mock<IConnectionMultiplexer> Multiplexer, Mock<IDatabase> Database) Create()
    {
        var state = new Dictionary<string, long>(StringComparer.Ordinal);

        var db = new Mock<IDatabase>(MockBehavior.Loose);
        db.Setup(x => x.StringIncrement(It.IsAny<RedisKey>(), 1L, CommandFlags.None))
            .Returns<RedisKey, long, CommandFlags>((key, _, _) =>
            {
                var s = key.ToString();
                state.TryGetValue(s, out var current);
                current++;
                state[s] = current;
                return current;
            });
        db.Setup(x => x.KeyExpire(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), ExpireWhen.Always, CommandFlags.None))
            .Returns(true);
        db.Setup(x => x.KeyExpire(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), CommandFlags.None))
            .Returns(true);
        db.Setup(x => x.StringIncrementAsync(It.IsAny<RedisKey>(), 1L, CommandFlags.None))
            .Returns<RedisKey, long, CommandFlags>((key, _, _) =>
            {
                var s = key.ToString();
                state.TryGetValue(s, out var current);
                current++;
                state[s] = current;
                return Task.FromResult(current);
            });
        db.Setup(x => x.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), CommandFlags.None))
            .ReturnsAsync(true);
        db.Setup(x => x.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), ExpireWhen.Always, CommandFlags.None))
            .ReturnsAsync(true);
        db.Setup(x => x.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), CommandFlags.None))
            .ReturnsAsync(true);
        db.Setup(x => x.LockReleaseAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), CommandFlags.None))
            .ReturnsAsync(true);
        db.Setup(x => x.ListLeftPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), When.Always, CommandFlags.None))
            .ReturnsAsync(1L);
        db.Setup(x => x.ListTrimAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<long>(), CommandFlags.None))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        db.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        db.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>()))
            .ReturnsAsync(true);
        db.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);
        db.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var multiplexer = new Mock<IConnectionMultiplexer>(MockBehavior.Loose);
        multiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        return (multiplexer, db);
    }
}
