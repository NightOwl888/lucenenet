using Lucene.Net.Replicator.Http.Abstractions;
using System;

namespace Lucene.Net.Replicator.Http
{
    public class MockReplicationService : IReplicationService
    {
        private readonly Action<IReplicationRequest, IReplicationResponse> perform;

        public MockReplicationService(Action<IReplicationRequest, IReplicationResponse> perform)
        {
            if (perform == null)
                throw new ArgumentNullException("perform");
            this.perform = perform;
        }

        public void Perform(IReplicationRequest request, IReplicationResponse response)
        {
            perform(request, response);
        }
    }
}
