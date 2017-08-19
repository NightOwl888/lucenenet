using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Replicator.Http.Abstractions;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Replicator.Http
{
    public class TestReplicationHttpListener : LuceneTestCase
    {
        //private int port;
        //private string host;
        //private ReplicationHttpListener server;
        //private Action<IReplicationRequest, IReplicationResponse> perform;

        private enum HttpMethod
        {
            GET, POST
        }

        //private void StartServer()
        //{
        //    perform = (request, response) =>
        //    {
        //        assertEquals("foo", request.Path);
        //    };

        //    IReplicationService service = new MockReplicationService(perform);

        //    server = new ReplicationHttpListener(service, IPAddress.Loopback, 0);
        //    server.Start();
        //    var endPoint = ((IPEndPoint)server.TcpListener.LocalEndpoint);
        //    port = endPoint.Port;
        //    host = endPoint.Address.ToString();
        //}

        

        public override void SetUp()
        {
            base.SetUp();

            //StartServer();
        }

        public override void TearDown()
        {
            //server.Stop();

            base.TearDown();
        }

        private HttpWebResponse PerformGet(string pathAndQuery, Action<IReplicationRequest, IReplicationResponse> perform)
        {
            return PerformNetworkCall(HttpMethod.GET, pathAndQuery, "", perform);
        }
        private HttpWebResponse PerformPost(string pathAndQuery, string postData, Action<IReplicationRequest, IReplicationResponse> perform)
        {
            return PerformNetworkCall(HttpMethod.POST, pathAndQuery, postData, perform);
        }

        private HttpWebResponse PerformNetworkCall(HttpMethod httpMethod, string pathAndQuery, string postData, Action<IReplicationRequest, IReplicationResponse> perform)
        {
            // Setup server
            var server = new ReplicationHttpListener(new MockReplicationService(perform), IPAddress.Loopback, 0);
            server.Start();

            // Get Network Details
            var endPoint = ((IPEndPoint)server.TcpListener.LocalEndpoint);
            var port = endPoint.Port;
            var host = endPoint.Address.ToString();

            // Create Client Request
            var pathPlusQuery = (string.IsNullOrEmpty(pathAndQuery)) ? "" : 
                (pathAndQuery.StartsWith("/") ? pathAndQuery.Substring(1) : pathAndQuery);
            var request = (HttpWebRequest)WebRequest.Create(string.Format("http://{0}:{1}/{2}", host, port, pathPlusQuery));

            if (httpMethod == HttpMethod.POST)
            {
                var data = Encoding.ASCII.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            else
            {
                request.Method = "GET";
            }

            // Perform Network Call
            var response = (HttpWebResponse)request.GetResponse();
            //var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();


            // Shutdown Server
            server.Stop();

            return response;
        }

        [Test, LuceneNetSpecific]
        public void TestGetBasic()
        {
            PerformGet("foo/bar", 
                (request, response) =>
                {
                    assertEquals("foo/bar", request.Path);
                    assertEquals(null, request.QueryParam("x"));
                    assertEquals(null, request.QueryParam("y"));
                });
        }

        [Test, LuceneNetSpecific]
        public void TestGetWithQueryString()
        {
            PerformGet("foo/bar?z=oopsy&y=987&x=trendy&z=umph&encoded=%22Quoted%20String%22", 
                (request, response) =>
                {
                    assertEquals("foo/bar", request.Path);
                    assertEquals("trendy", request.QueryParam("x"));
                    assertEquals("987", request.QueryParam("y"));
                    assertEquals("oopsy,umph", request.QueryParam("z"));
                    assertEquals("\"Quoted String\"", request.QueryParam("encoded"));
                });
        }



        [Test, LuceneNetSpecific]
        public void TestFullIntegration()
        {
            // Setup working Directories
            var handlerIndexDir = NewDirectory();
            var serverIndexDir = NewDirectory();
            var clientWorkDir = CreateTempDir("replicator-client");

            // Setup Server
            var serverReplicator = new LocalReplicator();
            ReplicationService service = new ReplicationService(new Dictionary<string, IReplicator> { { "s1", serverReplicator } });

            var server = new ReplicationHttpListener(service, IPAddress.Loopback, 0);
            server.Start();

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            var writer = new IndexWriter(serverIndexDir, conf);
            var reader = DirectoryReader.Open(writer, false);


            // Get Network Details
            var endPoint = ((IPEndPoint)server.TcpListener.LocalEndpoint);
            var port = endPoint.Port;
            var host = endPoint.Address.ToString();

            // Setup Client
            IReplicator replicator = new HttpReplicator(host, port, ReplicationService.REPLICATION_CONTEXT + "/s1", null /*server.CreateHandler()*/);
            ReplicationClient client = new ReplicationClient(replicator, new IndexReplicationHandler(handlerIndexDir, null),
                new PerSessionDirectoryFactory(clientWorkDir.FullName));

            PublishRevision(writer, serverReplicator, 1);
            client.UpdateNow();
            ReopenReader(reader);
            assertEquals(1, int.Parse(reader.IndexCommit.UserData["ID"], NumberStyles.HexNumber));

            PublishRevision(writer, serverReplicator, 2);
            client.UpdateNow();
            ReopenReader(reader);
            assertEquals(2, int.Parse(reader.IndexCommit.UserData["ID"], NumberStyles.HexNumber));

            server.Stop();
        }





        private void PublishRevision(IndexWriter writer, IReplicator serverReplicator, int id)
        {
            Document doc = new Document();
            writer.AddDocument(doc);
            writer.SetCommitData(Collections.SingletonMap("ID", id.ToString("X")));
            writer.Commit();
            serverReplicator.Publish(new IndexRevision(writer));
        }

        private void ReopenReader(DirectoryReader reader)
        {
            DirectoryReader newReader = DirectoryReader.OpenIfChanged(reader);
            assertNotNull(newReader);
            reader.Dispose();
            reader = newReader;
        }
    }
}
