using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using FluentAssertions;
using LSG.Core;
using LSG.Hosts.LsgApi;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using LSG.SharedKernel.Nats;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client;
using NSubstitute;
using NUnit.Framework;
using TaskExtensions = LSG.SharedKernel.Extensions.TaskExtensions;

namespace LSG.IntegrationTests.Nats
{
    [TestFixture]
    public class NatsManagerTests : Base<LsgApiStartup>
    {
        [Test]
        public void CanReadConfigModel()
        {
            var natsConfig = DefaultFactory.GetRequiredService<INatsConfig>();
            natsConfig.Urls.Should().NotBeNullOrEmpty();
        }


        [Test]
        public async Task CanPublishAndSubscribeMessages()
        {
            const string topic = "testtopic";
            const string value = "Value";
            var taskSource = new TaskCompletionSource<string>();
            var natsManager = DefaultFactory.GetRequiredService<INatsManager>();
            using var dis = natsManager.SubscribeAsync<string>(topic, message =>
            {
                message.Should().Be(value);
                taskSource.SetResult(value);
            });
            natsManager.Publish(topic, value);

            await taskSource.Task.TimeoutAfterAsync(TimeSpan.FromSeconds(3));
            var result = await taskSource.Task;

            result.Should().Be(value);
        }

        [Test]
        public async Task CanPublishAndSubscribeObjectMessages()
        {
            const string topic = "testtopic2A";
            var taskSource = new TaskCompletionSource<TestObject>();
            var testObj = new TestObject
            {
                TestString = "testString",
                TestInt = 12345,
                TestClass = new InternalTestClass {InternalTestString = "internalTestString"}
            };
            var natsManager = DefaultFactory.GetRequiredService<INatsManager>();
            Console.WriteLine("nats manager hash" + natsManager.GetHashCode());
            using var dis = natsManager.SubscribeAsync<TestObject>(topic, message =>
            {
                message.Should().BeEquivalentTo(testObj);
                Console.WriteLine($@"received msg {message.ToJson()}");
                taskSource.SetResult(testObj);
            });
            natsManager.Publish(topic, testObj);

            await taskSource.Task.TimeoutAfterAsync(TimeSpan.FromSeconds(3));
            var result = await taskSource.Task;
            result.Should().BeEquivalentTo(testObj);
        }


        [Test]
        public void ShouldThrowWhenNotConnectToNats()
        {
            const string topic = "testtopic";
            const string value = "Value";


            var config = Substitute.For<INatsConfig>();
            var logger = Substitute.For<ILsgLogger>();
            config.Urls.Returns(new[] {"nats://na:4222"});

            INatsManager natsManager = new NatsManager(new NatsConnection(config, logger), logger);
            Action act = () => natsManager.SubscribeAsync<string>(topic, message => { message.Should().Be(value); });
            act.Should().Throw<NATSConnectionException>()
                .Where(a => a.Message.Contains("timeout"));
        }

        [TestCase(false), Category(Const.TestCategory.LocalOnly), Parallelizable(ParallelScope.None), Order(999),
         Ignore("test manually")]
        [TestCase(true), Category(Const.TestCategory.LocalOnly)]
        public async Task CanReConnectToNatsWhenDisconnect(bool isFireForget)
        {
            var client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
            var containers = await client.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    Limit = 10,
                });

            var nats = containers.FirstOrDefault(a => a.Names.Any(c => c.EndsWith("nats")));
            if (nats == null)
                throw new Exception("nats not running");

            await client.Containers.StartContainerAsync(
                nats.ID,
                new ContainerStartParameters());

            const string topic = "testtopic2";
            var taskSource = new TaskCompletionSource<TestObject>();
            var testObj = new TestObject
            {
                TestString = "testString",
                TestInt = 12345,
                TestClass = new InternalTestClass {InternalTestString = "internalTestString"}
            };
            var natsManager = DefaultFactory.GetRequiredService<INatsManager>();
            using var dis = natsManager.SubscribeAsync<TestObject>(topic, message =>
            {
                message.Should().BeEquivalentTo(testObj);
                taskSource.SetResult(testObj);
            });


            await client.Containers.StopContainerAsync(nats.ID, new ContainerStopParameters
            {
                WaitBeforeKillSeconds = 30
            });

            Func<Task> task = async () =>
            {
                var count = 1;
                while (count < 11)
                {
                    try
                    {
                        natsManager.Publish(topic, testObj, isFireForget);
                        await taskSource.Task.TimeoutAfterAsync(TimeSpan.FromSeconds(1));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($@"{DateTimeOffset.Now} {e.Message}=>{count}");
                        count++;
                        continue;
                    }

                    Console.WriteLine(@"leave while");

                    break;
                }
            };
            TaskExtensions.RunOnBackgroundAsync(task);

            await Task.Delay(TimeSpan.FromSeconds(2)); //restore connection

            await client.Containers.StartContainerAsync(
                nats.ID,
                new ContainerStartParameters());
            var result = await taskSource.Task.ConfigureAwait(false);
            result.TestClass.Should().BeEquivalentTo(testObj.TestClass);
            result.TestInt.Should().Be(testObj.TestInt);
            result.TestString.Should().BeEquivalentTo(testObj.TestString);
        }

        [Test, Repeat(2)]
        public async Task CanSubscribeBatchWithQueue()
        {
            const string topic = "testtopic2333";
            const int totalcount = 30;
            var count = 0;
            var currentCount = Interlocked.Exchange(ref count, 0);

            var taskSource = new TaskCompletionSource<DateTimeOffset>();

            var natsManager = DefaultFactory.GetRequiredService<INatsManager>();

            var (disposable11, disposable12) = natsManager
                .SubscribeBatchAsync<Counting>(topic,
                    message =>
                    {
                        Console.WriteLine($@"{DateTimeOffset.Now} => batch 1 process start");

                        foreach (var a in message)
                        {
                            Interlocked.Increment(ref currentCount);
                            Console.WriteLine(
                                $@"{DateTimeOffset.Now} =>batch 1 receive seq :{a.Seq} currentCount is  : {currentCount}");
                        }

                        if (currentCount == totalcount)
                        {
                            Console.WriteLine($@"{DateTimeOffset.Now} => batch 1 process last");

                            taskSource.SetResult(DateTimeOffset.Now);
                        }
                    }, TimeSpan.FromSeconds(1), 50, "testq");

            var (disposable21, disposable22) = natsManager
                .SubscribeBatchAsync<Counting>(topic,
                    message =>
                    {
                        Console.WriteLine($@"{DateTimeOffset.Now} => batch 2 process start");

                        foreach (var a in message)
                        {
                            Interlocked.Increment(ref currentCount);
                            Console.WriteLine(
                                $@"{DateTimeOffset.Now} =>batch 2 receive seq :{a.Seq} currentCount is  : {currentCount}");
                        }

                        if (currentCount == totalcount)
                        {
                            Console.WriteLine($@"{DateTimeOffset.Now} => batch 2 process last");

                            taskSource.SetResult(DateTimeOffset.Now);
                        }
                    }, TimeSpan.FromSeconds(1), 10, "testq");

            for (int i = 0; i < totalcount; i++)
            {
                natsManager.Publish(topic, new Counting {Seq = i});
            }

            Console.WriteLine($@"{DateTimeOffset.Now} publish done ");
            await taskSource.Task.TimeoutAfterAsync(TimeSpan.FromSeconds(3));


            Console.WriteLine($@"done at {await taskSource.Task.ConfigureAwait(false)}");
            disposable11.Dispose();
            disposable12.Dispose();
            disposable21.Dispose();
            disposable22.Dispose();
        }

        [Test, Repeat(10)]
        public async Task CanSubscribeBatch()
        {
            const string topic = "CanSubscribeBatch";
            const int totalcount = 30;

            var count = 0;
            var currentCount = Interlocked.Exchange(ref count, 0);
            var taskSource = new TaskCompletionSource<DateTimeOffset>();

            var natsManager = DefaultFactory.GetRequiredService<INatsManager>();
            Console.WriteLine("nats manager hash" + natsManager.GetHashCode());
            var (dis1, dis2) =
                natsManager
                    .SubscribeBatchAsync<Counting>(topic,
                        message =>
                        {
                            Console.WriteLine(
                                $@"{DateTimeOffset.Now} => batch 1 process start process total counts : {message.Length}");

                            foreach (var a in message)
                            {
                                Interlocked.Increment(ref currentCount);
                                Console.WriteLine(
                                    $@"{DateTimeOffset.Now} =>batch 1 receive seq :{a.Seq} currentCount is  : {currentCount}");
                            }

                            if (currentCount == totalcount)
                            {
                                Console.WriteLine($@"{DateTimeOffset.Now} => batch 1 process last");

                                taskSource.SetResult(DateTimeOffset.Now);
                            }
                        }
                        , TimeSpan.FromSeconds(1), 2);


            for (int i = 0; i < totalcount; i++)
            {
                natsManager.Publish(topic, new Counting {Seq = i});
                Console.WriteLine($@"{DateTime.Now} published {i} ");
            }


            await taskSource.Task.TimeoutAfterAsync(TimeSpan.FromSeconds(3));


            Console.WriteLine($@"done at {await taskSource.Task.ConfigureAwait(false)}");
            dis1.Dispose();
            dis2.Dispose();
        }


        public class Counting
        {
            public int Seq { set; get; }
        }

        public class TestObject
        {
            public string TestString { set; get; }
            public int TestInt { set; get; }
            public InternalTestClass TestClass { set; get; }
        }


        public class InternalTestClass
        {
            public string InternalTestString { set; get; }
        }
    }
}