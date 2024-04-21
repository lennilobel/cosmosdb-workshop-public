using System;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.Shared
{
	public static class MultithreadingHelper
	{
		public static void Execute(Action[] methods)
		{
			var tasks = new Task[methods.Length];
			for (var i = 0; i < methods.Length; i++)
			{
				var task = new Task(methods[i]);
				task.Start();
				tasks[i] = task;
			}
			Task.WaitAll(tasks.ToArray());
		}

		public static TOut[] ExecuteLoop<TIn, TOut>(TIn[] source, Func<TIn, TOut> method, int threadCount = 1)
		{
			var transformed = new TOut[source.Length];

			var offset = 0;

			while (true)
			{
				var batch = source.Skip(offset).Take(threadCount).ToArray();
				if (batch.Length == 0)
				{
					break;
				}

				var tasks = new Task[batch.Length];
				for (var i = 0; i < batch.Length; i++)
				{
					var absoluteIndex = i + offset;
					var relativeIndex = i;
					var task = new Task(new Action(() => transformed[absoluteIndex] = method(batch[relativeIndex])));
					task.Start();
					tasks[i] = task;
				}
				offset += threadCount;

				Task.WaitAll(tasks.ToArray());
			}

			return transformed;
		}

		public static void ExecuteLoop<T>(T[] source, Action<T> method, int threadCount = 1)
		{
			var offset = 0;

			while (true)
			{
				var batch = source.Skip(offset).Take(threadCount).ToArray();
				if (batch.Length == 0)
				{
					break;
				}

				var tasks = new Task[batch.Length];
				for (var i = 0; i < batch.Length; i++)
				{
					var absoluteIndex = i + offset;
					var relativeIndex = i;
					var task = new Task(new Action(() => method(batch[relativeIndex])));
					task.Start();
					tasks[i] = task;
				}
				offset += threadCount;

				Task.WaitAll(tasks.ToArray());
			}
		}

	}
}
