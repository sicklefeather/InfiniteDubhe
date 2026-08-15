using System.Text;
using InfiniteDubhe.Core;
using Xunit;

namespace InfiniteDubhe.Core.Tests;

public sealed class ObjectPoolTests
{
    [Fact]
    public void Rent_ReusesInstance_AfterReturn()
    {
        int created = 0;
        var pool = new ObjectPool<StringBuilder>(() => { created++; return new StringBuilder(); });

        var a = pool.Rent();
        Assert.Equal(1, created);
        a.Append("hello");

        pool.Return(a);
        Assert.Equal(1, pool.Count);

        var b = pool.Rent();
        Assert.Same(a, b);          // 复用同一实例
        Assert.Equal(1, created);   // 未新建
    }
}
