using System;
using System.Reflection;
using System.Threading.Tasks;
using Castle.Core;
using Castle.Core.Interceptor;
using Castle.DynamicProxy;

namespace AsyncWindsorProxy
{
    public class Executer<TReturnType>
    {
        private readonly MethodInfo _targetMethod;
        private readonly object _target;
        private readonly object[] _args;

        public Executer(MethodInfo targetMethod, object target, object[] args)
        {
            _targetMethod = targetMethod;
            _target = target;
            _args = args;
        }

        public TReturnType Execute()
        {
            return (TReturnType)_targetMethod.Invoke(_target, BindingFlags.InvokeMethod, null, _args, null);
        }
    }

    public class AsyncInterceptor<T> : IInterceptor, IOnBehalfAware 
    {
        private readonly T _target;

        public AsyncInterceptor(T target)
        {
            _target = target;
        }

        public void Intercept(IInvocation invocation)
        {
            var methodName = invocation.Method.Name;
            var targetMethod = typeof (T).GetMethod(methodName);
            var returnType = targetMethod.ReturnType;
            var functionType = typeof (Func<>).MakeGenericType(returnType);

            var executertype = typeof (Executer<>).MakeGenericType(returnType);

            var executeMethod = executertype.GetMethod("Execute");

            var executer = Activator.CreateInstance(executertype, targetMethod, _target, invocation.Arguments);

            dynamic function = Delegate.CreateDelegate(functionType, executer, executeMethod);

            var factoryType = typeof (Task<>).MakeGenericType(returnType);
            var factoryProperty = factoryType.GetProperty("Factory");
            dynamic factory = factoryProperty.GetValue(null);

            var task = factory.StartNew(function);

            invocation.ReturnValue = task;
        }

        public void SetInterceptedComponentModel(ComponentModel target)
        {
            throw new NotImplementedException();
        }
    }
}
