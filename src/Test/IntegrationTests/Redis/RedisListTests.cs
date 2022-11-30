using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LSG.Hosts.LsgFrontend;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Redis;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LSG.IntegrationTests.Redis
{
    [TestFixture]
    public class RedisListTests : Base<LsgFrontendStartup>
    {
        private IRedisList<Stub> _redisList;


        public override async Task Init()
        {
            await base.Init();
            _redisList = DefaultFactory.GetRequiredService<IRedisList<Stub>>();
        }


        [TearDown]
        public Task TearDownAsync()
        {
            return _redisList.ClearAsync();
        }

        [Test]
        public async Task CountAsync_Returns_Zero_For_Newly_Created()
        {
            var count = await _redisList.CountAsync();
            count.Should().Be(0);
        }

        [Test]
        public async Task Can_add_item_to_start()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));

            var count = await _redisList.CountAsync();
            count.Should().Be(2);
        }

        [Test]
        public async Task Can_add_multiple_items_to_start()
        {
            var items = new[]
            {
                new Stub("Test1"),
                new Stub("Test2"),
                new Stub("Test3")
            };

            await _redisList.AddToStartAsync(items);

            var count = await _redisList.CountAsync();
            count.Should().Be(items.Length);
        }

        [Test]
        public async Task Can_add_item_to_end()
        {
            await _redisList.AddToEndAsync(new Stub("Test1"));
            await _redisList.AddToEndAsync(new Stub("Test2"));

            var count = await _redisList.CountAsync();
            count.Should().Be(2);
        }

        [Test]
        public async Task Can_add_multiple_items_to_end()
        {
            var items = new[]
            {
                new Stub("Test1"),
                new Stub("Test2"),
                new Stub("Test3")
            };

            await _redisList.AddToEndAsync(items);

            var count = await _redisList.CountAsync();
            count.Should().Be(items.Length);
        }


        [Test]
        public async Task Can_clear_list()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToEndAsync(new Stub("Test2"));
            await _redisList.ClearAsync();

            var count = await _redisList.CountAsync();
            count.Should().Be(0);
        }

        [Test]
        public async Task Shrink_empty_do_nothing()
        {
            await _redisList.ShrinkAsync(5);

            var count = await _redisList.CountAsync();
            count.Should().Be(0);
        }

        [Test]
        public async Task Shrink_when_not_enough_do_nothing()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.ShrinkAsync(5);

            var count = await _redisList.CountAsync();
            count.Should().Be(2);
        }

        [Test]
        public async Task Shrink_when_enough_shrinks_list()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));
            await _redisList.AddToStartAsync(new Stub("Test4"));
            await _redisList.AddToStartAsync(new Stub("Test5"));
            await _redisList.ShrinkAsync(3);

            var count = await _redisList.CountAsync();
            count.Should().Be(3);

            var all = await _redisList.TakeAsync(3);
            all.Should().BeEquivalentTo(
                new []{new Stub("Test5"), new Stub("Test4"), new Stub("Test3")}
                );
        }

        [Test]
        public async Task GetRange_when_empty_returns_empty()
        {
            var range = await _redisList.GetRangeAsync(0, 5);

            range.Should().NotBeNull();
            range.Length.Should().Be(0);
        }

        [Test]
        public async Task GetRange_when_not_enough_returns_all()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));

            var range = await _redisList.GetRangeAsync(0, 5);

            range.Length.Should().Be(3);
        }

        [Test]
        public async Task GetRange_when_enough_returns_as_requested()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));

            var range = await _redisList.GetRangeAsync(0, 2);

            range.Length.Should().Be(2);
            range.Should().BeEquivalentTo(new []{new Stub("Test3"), new Stub("Test2")});
        }

        [Test]
        public async Task GetRange_when_enough_returns_as_requested_end()
        {
            await _redisList.AddToEndAsync(new Stub("Test1"));
            await _redisList.AddToEndAsync(new Stub("Test2"));
            await _redisList.AddToEndAsync(new Stub("Test3"));

            var range = await _redisList.GetRangeAsync(0, 2);

            range.Length.Should().Be(2);
            range.Should().BeEquivalentTo(new []{new Stub("Test1"), new Stub("Test2")});
        }

        [Test]
        public async Task GetRange_when_not_from_start_returns_as_requested()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));

            var range = await _redisList.GetRangeAsync(1, 2);

            range.Length.Should().Be(2);
            range.Should().BeEquivalentTo(new []{new Stub("Test2"), new Stub("Test1")});
        }

        [Test]
        public async Task GetRange_when_not_from_start_returns_as_requested_end()
        {
            await _redisList.AddToEndAsync(new Stub("Test1"));
            await _redisList.AddToEndAsync(new Stub("Test2"));
            await _redisList.AddToEndAsync(new Stub("Test3"));

            var range = await _redisList.GetRangeAsync(1, 2);

            range.Length.Should().Be(2);
            range.Should().BeEquivalentTo(new []{new Stub("Test2"), new Stub("Test3")});
        }

        [Test]
        public async Task Take_when_empty_returns_empty()
        {
            var range = await _redisList.TakeAsync(5);

            range.Should().NotBeNull();
            range.Length.Should().Be(0);
        }

        [Test]
        public async Task Take_when_empty_returns_empty_with_skip()
        {
            var range = await _redisList.TakeAsync(5, 3);

            range.Should().NotBeNull();
            range.Length.Should().Be(0);
        }

        [Test]
        public async Task Take_when_not_enough_returns_all()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));

            var range = await _redisList.TakeAsync(5);

            range.Length.Should().Be(3);
        }

        [Test]
        public async Task Take_when_enough_returns_as_requested()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));

            var range = await _redisList.TakeAsync(2);

            range.Length.Should().Be(2);
            range.Should().BeEquivalentTo(new []{new Stub("Test3"), new Stub("Test2")});
        }

        [Test]
        public async Task Take_when_not_from_start_returns_as_requested()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));

            var range = await _redisList.TakeAsync(2, 1);

            range.Length.Should().Be(2);
            range.Should().BeEquivalentTo(new []{new Stub("Test2"), new Stub("Test1")});
        }

        [Test]
        public async Task Take_when_not_from_start_returns_as_requested_with_skip_over_boundry()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));

            var range = await _redisList.TakeAsync(2, 2);

            range.Length.Should().Be(1);
            range.Should().BeEquivalentTo(new []{new Stub("Test1")});
        }

        [Test]
        public async Task Take_when_not_from_start_returns_as_requested_with_skip_over_boundry_end()
        {
            await _redisList.AddToEndAsync(new Stub("Test1"));
            await _redisList.AddToEndAsync(new Stub("Test2"));
            await _redisList.AddToEndAsync(new Stub("Test3"));

            var range = await _redisList.TakeAsync(2, 2);

            range.Length.Should().Be(1);
            range.Should().BeEquivalentTo(new []{new Stub("Test3")});
        }


        [Test]
        public async Task Can_get_item_from_start()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToEndAsync(new Stub("Test2"));

            var item = await _redisList.PopFromStartAsync();
            item.Should().NotBeNull();
            item.Should().BeEquivalentTo(new Stub("Test1"));

            var count = await _redisList.CountAsync();
            count.Should().Be(1);
        }

        [Test]
        public async Task Can_get_empty_item_from_start()
        {
            var item = await _redisList.PopFromStartAsync();
            item.Should().BeNull();
        }

        [Test]
        public async Task Can_get_item_from_end()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToEndAsync(new Stub("Test2"));

            var item = await _redisList.PopFromEndAsync();
            item.Should().NotBeNull();
            item.Should().BeEquivalentTo(new Stub("Test2"));

            var count = await _redisList.CountAsync();
            count.Should().Be(1);
        }

        [Test]
        public async Task Can_get_empty_item_from_end()
        {
            var item = await _redisList.PopFromEndAsync();
            item.Should().BeNull();
        }

        [Test]
        public async Task GetRangeAndShrinkIfGreaterThanCount()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));
            await _redisList.AddToStartAsync(new Stub("Test4"));

            await _redisList.ShrinkAsync(2);

            var range = await _redisList.GetRangeAsync(0, 2);

            range.Length.Should().Be(2);

            range.Reverse().Should().BeEquivalentTo(new []{new Stub("Test3"), new Stub("Test4")});
        }

        [Test]
        public async Task CanRemoveItem()
        {
            await _redisList.AddToStartAsync(new Stub("Test1"));
            await _redisList.AddToStartAsync(new Stub("Test2"));
            await _redisList.AddToStartAsync(new Stub("Test3"));
            await _redisList.AddToStartAsync(new Stub("Test4"));

            await _redisList.RemoveAsync(new Stub("Test2"));

            var range = await _redisList.TakeAsync(5);

            Console.WriteLine(range.ToJson());
            range.Length.Should().Be(3);

            range.Should().NotContain(new Stub("Test2"));
        }

        public class Stub
        {
            public Stub()
            {
            }

            public Stub(string name)
            {
                Name = name;
            }

            public string Name { get; set; }
        }
    }
}