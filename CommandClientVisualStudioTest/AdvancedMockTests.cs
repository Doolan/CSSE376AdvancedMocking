using System;
using System.Net;
using System.Reflection;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proshot.CommandClient;
using Rhino.Mocks;
using System.Linq;

namespace CommandClientVisualStudioTest
{
    [TestClass]
    public class AdvancedMockTests
    {
        private MockRepository mocks;

        [TestMethod]
        public void VerySimpleTest()
        {
            CMDClient client = new CMDClient(null, "Bogus network name");
            Assert.AreEqual("Bogus network name", client.NetworkName);
        }

        [TestInitialize()]
        public void Initialize()
        {
            mocks = new MockRepository();
        }

        [TestMethod]
        public void TestUserExitCommand()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");

            typeof(CMDClient).GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(client, fakeStream);
            client.SendCommandToServerUnthreaded(command);
            mocks.VerifyAll();

        }

        [TestMethod]
        public void TestUserExitCommandWithoutMocks()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = new MemoryStream();

            CMDClient client = new CMDClient(null, "Bogus network name");

            typeof(CMDClient).GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(client, fakeStream);
            client.SendCommandToServerUnthreaded(command);
            ///Console.Out.Write(Convert.ToString(((MemoryStream) fakeStream).GetBuffer()));
        }

        [TestMethod]
        public void TestSemaphoreReleaseOnNormalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            CMDClient client = new CMDClient(null, "Bogus network name");
            System.Threading.Semaphore fakeSemaphore = mocks.DynamicMock<System.Threading.Semaphore>();

            //mocking
            System.IO.Stream fakeStream = new MemoryStream();
            typeof(CMDClient).GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(client, fakeStream);
            typeof(CMDClient).GetField("semaphore", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(client, fakeSemaphore);


            using (mocks.Ordered())
            {
                Expect.Call(fakeSemaphore.WaitOne()).Return(true);
                Expect.Call(fakeSemaphore.Release()).Return(0);
            }
            mocks.ReplayAll();
            client.SendCommandToServerUnthreaded(command);

            mocks.VerifyAll();
        }

        [TestMethod]
        public void TestSemaphoreReleaseOnExceptionalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            CMDClient client = new CMDClient(null, "Bogus network name");
            System.Threading.Semaphore fakeSemaphore = mocks.DynamicMock<System.Threading.Semaphore>();

            //mocking
            //System.IO.Stream fakeStream = new MemoryStream();


            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            typeof(CMDClient).GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(client, fakeStream);
            typeof(CMDClient).GetField("semaphore", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(client, fakeSemaphore);

            using (mocks.Ordered())
            {
                Expect.Call(fakeSemaphore.WaitOne()).Return(true);
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                try
                {
                    fakeStream.Flush();
                    LastCall.On(fakeStream).Throw(new Exception("expected error"));
                }
                catch (Exception e)
                {
                    Expect.Call(fakeSemaphore.Release()).Return(0);
                }
            }
            mocks.ReplayAll();
            try
            {
                client.SendCommandToServerUnthreaded(command);

            }
            catch (Exception e)
            {
                mocks.VerifyAll();
            }
        }
    }
}
