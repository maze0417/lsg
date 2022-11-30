using System.Threading.Tasks;
using FluentAssertions;
using LSG.SharedKernel.Logger;
using NUnit.Framework;

namespace LSG.UnitTests
{
    [TestFixture]
    public class CallerContextTests
    {
        [Test, Repeat(1000)]
        public void WhenFlowingDataThenCanUseContext()
        {
            var d1 = new object();
            var t1 = default(object);

            var t12 = default(object);
            var t13 = default(object);
            var d2 = new object();
            var t2 = default(object);

            var t22 = default(object);
            var t23 = default(object);

            Task.WaitAll(
                Task.Run(() =>
                {
                    CallContext<object>.SetData("d1", d1);
                    Task.WaitAll(
                        Task.Run(() => t1 = CallContext<object>.GetData("d1")),
                        Task.Run(() => t12 = CallContext<object>.GetData("d1")),
                        Task.Run(() => t13 = CallContext<object>.GetData("d1"))
                    );
                }),
                Task.Run(() =>
                {
                    CallContext<object>.SetData("d2", d2);
                    Task.WaitAll(
                        Task.Run(() => t2 = CallContext<object>.GetData("d2")),
                        Task.Run(() => t22 = CallContext<object>.GetData("d2")),
                        Task.Run(() => t23 = CallContext<object>.GetData("d2"))
                    );
                })
            );


            d1.Should().Be(t1);
            d1.Should().Be(t12);
            d1.Should().Be(t13);

            d2.Should().Be(t2);

            d2.Should().Be(t22);
            d2.Should().Be(t23);

            Assert.Null(CallContext<object>.GetData("d1"));
            Assert.Null(CallContext<object>.GetData("d2"));
        }
    }
}