using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Support.IO
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    public class TestDataInputStream : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public void TestReadFully()
        {
            const string READFULLY_TEST_FILE = "ReadFully.txt";
            int fileLength;
            Stream @in;

            // Read one time to measure the length of the file (it may be different 
            // on different operating systems because of line endings)
            using (@in = GetType().getResourceAsStream(READFULLY_TEST_FILE))
            {
                using (var ms = new MemoryStream())
                {
                    @in.CopyTo(ms);
                    fileLength = ms.ToArray().Length;
                }
            }

            // Declare the buffer one byte too large
            byte[] buffer = new byte[fileLength + 1];
            @in = GetType().getResourceAsStream(READFULLY_TEST_FILE);
            DataInputStream dis;
            using (dis = new DataInputStream(@in))
            { 
                // Read once for real (to the exact length)
                dis.ReadFully(buffer, 0, fileLength);
            }

            // Read past the end of the stream
            @in = GetType().getResourceAsStream(READFULLY_TEST_FILE);
            dis = new DataInputStream(@in);
            bool caughtException = false;
            try
            {
                // Using the buffer length (that is 1 byte too many)
                // should generate EndOfStreamException.
                dis.ReadFully(buffer, 0, buffer.Length);
            }
#pragma warning disable 168
            catch (EndOfStreamException ie)
#pragma warning restore 168
            {
                caughtException = true;
            }
            finally
            {
                dis.Dispose();
                if (!caughtException)
                    fail("Test failed");
            }

            // Ensure we get an IndexOutOfRangeException exception when length is negative
            @in = GetType().getResourceAsStream(READFULLY_TEST_FILE);
            dis = new DataInputStream(@in);
            caughtException = false;
            try
            {
                dis.ReadFully(buffer, 0, -20);
            }
#pragma warning disable 168
            catch (IndexOutOfRangeException ie)
#pragma warning restore 168
            {
                caughtException = true;
            }
            finally
            {
                dis.Dispose();
                if (!caughtException)
                    fail("Test failed");
            }
        }

        [Test, LuceneNetSpecific]
        public void TestReadLinePushback()
        {
            using (MemoryStream pis = new MemoryStream("\r".GetBytes(Encoding.UTF8)))
            {
                DataInputStream dis = new DataInputStream(pis);

#pragma warning disable 612, 618
                string line = dis.ReadLine();
#pragma warning restore 612, 618
                if (line == null)
                {
                    fail("Got null, should return empty line");
                }

                long count = pis.Length - (line.Length + 1 /*account for the newline*/);

                if (count != 0)
                {
                    fail("Test failed: available() returns "
                                         + count + " when the file is empty");
                }
            }
        }

        [Test, LuceneNetSpecific]
        public void TestReadUTF()
        {
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                try
                {
                    WriteAndReadAString();
                }
                catch (FormatException utfdfe)
                {
                    if (utfdfe.Message == null)
                        fail("vague exception thrown");
                }
#pragma warning disable 168
                catch (EndOfStreamException eofe)
#pragma warning restore 168
                {
                    // These are rare and beyond the scope of the test
                }
            }
        }


        private static readonly int TEST_ITERATIONS = 1000;

        private static readonly int A_NUMBER_NEAR_65535 = 60000;

        private static readonly int MAX_CORRUPTIONS_PER_CYCLE = 3;

        private static void WriteAndReadAString()
        {
            // Write out a string whose UTF-8 encoding is quite possibly
            // longer than 65535 bytes
            int length = Random.nextInt(A_NUMBER_NEAR_65535) + 1;
            MemoryStream baos = new MemoryStream();
            StringBuilder testBuffer = new StringBuilder();
            for (int i = 0; i < length; i++)
                testBuffer.append((char)Random.Next());
            string testString = testBuffer.toString();
            DataOutputStream dos = new DataOutputStream(baos);
            dos.WriteUTF(testString);

            // Corrupt the data to produce malformed characters
            byte[] testBytes = baos.ToArray();
            int dataLength = testBytes.Length;
            int corruptions = Random.nextInt(MAX_CORRUPTIONS_PER_CYCLE);
            for (int i = 0; i < corruptions; i++)
            {
                int index = Random.nextInt(dataLength);
                testBytes[index] = (byte)Random.Next();
            }

            // Pay special attention to mangling the end to produce
            // partial characters at end
            testBytes[dataLength - 1] = (byte)Random.Next();
            testBytes[dataLength - 2] = (byte)Random.Next();

            // Attempt to decode the bytes back into a String
            MemoryStream bais = new MemoryStream(testBytes);
            DataInputStream dis = new DataInputStream(bais);
            dis.ReadUTF();
        }

        [Test, LuceneNetSpecific]
        public void TestSkipBytes()
        {
            DataInputStream dis = new DataInputStream(new MyInputStream());
            dotest(dis, 0, 11, -1, 0);
            dotest(dis, 0, 11, 5, 5);
            Console.WriteLine("\n***CAUTION**** - may go into an infinite loop");
            dotest(dis, 5, 11, 20, 6);
        }


        private static void dotest(DataInputStream dis, int pos, int total,
                               int toskip, int expected)
        {

            try
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\n\nTotal bytes in the stream = " + total);
                    Console.WriteLine("Currently at position = " + pos);
                    Console.WriteLine("Bytes to skip = " + toskip);
                    Console.WriteLine("Expected result = " + expected);
                }
                int skipped = dis.SkipBytes(toskip);
                if (VERBOSE)
                {
                    Console.WriteLine("Actual skipped = " + skipped);
                }
                if (skipped != expected)
                {
                    fail("DataInputStream.skipBytes does not return expected value");
                }
            }
#pragma warning disable 168
            catch (EndOfStreamException e)
#pragma warning restore 168
            {
                fail("DataInputStream.skipBytes throws unexpected EOFException");
            }
#pragma warning disable 168
            catch (IOException e)
#pragma warning restore 168
            {
                Console.WriteLine("IOException is thrown - possible result");
            }
        }

        internal class MyInputStream : MemoryStream
        {

            private int readctr = 0;


            public override int ReadByte()
            {

                if (readctr > 10)
                {
                    return -1;
                }
                else
                {
                    readctr++;
                    return 0;
                }

            }

        }
    }
}
