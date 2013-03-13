using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncWindsorProxy;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rhino.Mocks;

namespace AsyncWindsorProxyTests
{
    public class Caller
    {
        private readonly IAsync _sync;

        public Caller(IAsync sync)
        {
            _sync = sync;
        }

        public int CallAsync(int x)
        {
            var task = _sync.Foo(x);
            task.Wait();
            return task.Result;
        }
}

    public interface ISync
    {
        int Foo (int x);
    }

    public interface IAsync
    {
        Task<int> Foo(int x);
    }

    [TestFixture]
    public class AsyncInterceptorTests
    {
        [Test]
        public void InterceptorWillCallTarget()
        {
            var container = new WindsorContainer();

            container.Register(
                Component.For<Caller>(),
           
                Component.For<AsyncInterceptor<ISync>>(),
                Component.For<IAsync>().Interceptors<AsyncInterceptor<ISync>>(),

                Component.For<ISync>().UsingFactoryMethod(()=> MockRepository.GenerateStub<ISync>())
                );

            var caller = container.Resolve<Caller>();
            var sync = container.Resolve<ISync>();

            sync.Stub(s => s.Foo(5)).Return(6);

            Assert.AreEqual(6, caller.CallAsync(5));
        }
    }
}
