using System.Collections.Concurrent;

namespace GrpcStateServiceProvider;
public static class ServerSideStateBag
{
    public static ConcurrentDictionary<string, byte[]> State = new ConcurrentDictionary<string, byte[]>();
}
