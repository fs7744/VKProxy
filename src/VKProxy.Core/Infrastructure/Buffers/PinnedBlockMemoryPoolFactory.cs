using System.Buffers;

namespace VKProxy.Core.Buffers;

public class PinnedBlockMemoryPoolFactory
{
    public static MemoryPool<byte> Create(int blockSize = 4096)
    {
        return new PinnedBlockMemoryPool(blockSize);
    }
}