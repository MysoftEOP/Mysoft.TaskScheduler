using Microsoft.Extensions.DependencyInjection;
using Mysoft.TaskScheduler;
using Mysoft.TaskScheduler.Handler;
using System;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            IServiceCollection serviceCollection = new ServiceCollection();

            var services = serviceCollection
                .AddTaskScheduler(new TaskSchedulerOptions
                {
                    BuilderLifetime = ServiceLifetime.Singleton,
                    EnableConsoleLog = true,
                    //OnlyClient = true,
                    RedisConnectionStrings = "localhost:6379,password=howe,abortConnect=false",
                    RedisDbIndex = 0,
                    RedisPrefix = "test",
                    RetryCount = 0,
                    ServerName = "localhost",
                    WorkerCount = 10,
                })
                .AddTransient<Task1>()
                .AddTransient<Task2>()
                .AddTransient<Task3>()
                .AddTransient<Task3Model>()
                .AddTransient<CallbackHandler>()
                .BuildServiceProvider()
                ;

            var builder = services.GetService<ITaskBuilder>();

            builder
                .Enqueue<Task1>()
                .Enqueue<Task2>()
                .Enqueue<Task3, Task3Model>(new Task3Model
                {
                    IntValue = 10,
                    StrValue = "hello world"
                })
                .Build<CallbackHandlerModel, CallbackHandler>(new CallbackHandlerModel
                {
                    Result = true
                });

            Console.ReadKey();
        }
    }

    public class Task1 : SimpleTask
    {
        public override void Do()
        {
            Console.WriteLine("Task1 Do.");

            throw new Exception("error");
        }

        public override void Undo()
        {
            Console.WriteLine("Task1 Undo.");
        }
    }

    public class Task2 : SimpleTask
    {
        public override void Do()
        {
            Console.WriteLine("DelayTask Do Begin.");
            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
            Console.WriteLine("DelayTask Do End.");
        }

        public override void Undo()
        {
            Console.WriteLine("DelayTask Undo Begin.");
            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
            Console.WriteLine("DelayTask Undo End.");
        }
    }

    public class Task3 : ParameterizedTask<Task3Model>
    {
        public override void Do(Task3Model model)
        {
            Console.WriteLine($"ParameterizedTask Do ### StrValue is {model.StrValue} - IntValue is {model.IntValue} ###");
        }

        public override void Undo(Task3Model model)
        {
            Console.WriteLine($"ParameterizedTask Undo ### StrValue is {model.StrValue} - IntValue is {model.IntValue} ###");
        }
    }

    public class Task3Model
    {
        public string StrValue { get; set; }

        public int IntValue { get; set; }
    }

    public class CallbackHandler : ITaskHandler<CallbackHandlerModel>
    {
        public Task DoFailed(CallbackHandlerModel model, Exception ex)
        {
            Console.WriteLine($"DoFailed ### result is {model.Result} ###");

            Console.WriteLine($"DoFailed ### ex is {ex.Message} ###");
            return Task.CompletedTask;
        }

        public Task DoFailed(CallbackHandlerModel model)
        {
            Console.WriteLine($"DoFailed ### result is {model.Result} ###");
            return Task.CompletedTask;
        }

        public Task DoFinished(CallbackHandlerModel model)
        {
            Console.WriteLine($"DoFinished ### result is {model.Result} ###");
            return Task.CompletedTask;
        }

        public Task DoSuccess(CallbackHandlerModel model)
        {
            Console.WriteLine($"DoSuccess ### result is {model.Result} ###");
            return Task.CompletedTask;
        }
    }

    public class CallbackHandlerModel
    {
        public bool Result { get; set; }
    }
}
