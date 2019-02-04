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
            var proxy = new DynamicProxyBuilder<ISomeWorker>()
                .AddPreAction(() =>
                {
                    Console.WriteLine("Pre Action Started");
                })
                .AddPostAction(() =>
                {
                    Console.WriteLine("Post Action Started");
                })
                .Build(worker);
            
            proxy.DoWork("Do Some useful work");
            Console.ReadLine();
        }
    }
}