using System;
using Vp.DynamicProxy;

namespace Vp.Sample
{
     public class Program
    {
        public interface ISomeWorker
        {
            void DoWork(string message);
        }

        public class Worker : ProxyWrapper<ISomeWorker>, ISomeWorker
        { 
            public void DoWork(string message)
            {
                try
                {
                    NullHandler(1, "a");
   
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
                
                ProxyObject.DoWork(message);
            }

            private string NullHandler(int a, string success)
            {
                return success + a;
            }

        }
        
        public class WorkerTest : ISomeWorker
        { 
            public void DoWork(string message)
            {                
                Console.WriteLine(message);
            }

        }
        
        
        static void Main(string[] args)
        {
            var worker = new WorkerTest();
            TypedReference typedReference = __makeref(worker);
            var proxy = new DynamicProxyBuilder<ISomeWorker>()
                .AddPreAction(() =>
                {
                    Console.WriteLine("Pre Action");
                })
                .Build(worker);
            proxy.DoWork("Do Work action invocation");
            Console.ReadLine();
        }
    }
}