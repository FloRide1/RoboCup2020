using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Alea;
using Alea.Parallel;

namespace Samples.CSharp
{
    class ParallelForTest
    {
        private const int Length = 1000000;

        [Test]
        public static void DelegateWithClosureCpu()
        {
            var arg1 = Enumerable.Range(0, Length).ToArray();
            var arg2 = Enumerable.Range(0, Length).ToArray();
            var result = new int[Length];

            Parallel.For(0, result.Length, i => result[i] = arg1[i] + arg2[i]);

            var expected = arg1.Zip(arg2, (x, y) => x + y);

            Assert.That(result, Is.EqualTo(expected));
        }

        [GpuManaged, Test]
        public static void DelegateWithClosureGpu()
        {
            var arg1 = Enumerable.Range(0, Length).ToArray();
            var arg2 = Enumerable.Range(0, Length).ToArray();
            var result = new int[Length];

            Gpu.Default.For(0, result.Length, i => result[i] = arg1[i] + arg2[i]);

            var expected = arg1.Zip(arg2, (x, y) => x + y);

            Assert.That(result, Is.EqualTo(expected));
        }

        [GpuManaged, Test]
        public static void ActionWithClosure()
        {
            var gpu = Gpu.Default;
            var arg1 = Enumerable.Range(0, Length).ToArray();
            var arg2 = Enumerable.Range(0, Length).ToArray();
            var result = new int[Length];

            Action<int> op = i => result[i] = arg1[i] + arg2[i];

            gpu.For(0, arg1.Length, op);

            var expected = arg1.Zip(arg2, (x, y) => x + y);

            Assert.That(result, Is.EqualTo(expected));
        }

        [GpuManaged, Test]
        public static void ActionFactoryWithClosure()
        {
            var gpu = Gpu.Default;
            var arg1 = Enumerable.Range(0, Length).ToArray();
            var arg2 = Enumerable.Range(0, Length).ToArray();
            var result = new int[Length];
            var expected = new int[Length];

            Func<int[], Action<int>> opFactory = res => i => res[i] = arg1[i] + arg2[i];

            gpu.For(0, arg1.Length, opFactory(result));

            Parallel.For(0, arg1.Length, opFactory(expected));

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
