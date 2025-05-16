using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class LogMessagesBackgroundService : BackgroundService
{
	private readonly MessageStore _store;

	public LogMessagesBackgroundService(MessageStore store)
	{
		_store = store;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			// Ждём 1 минуту
			await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

			var messages = _store.GetAndClear();
			if (messages.Count == 0)
			{
				Console.WriteLine("Нет новых сообщений для логирования.");
				continue;
			}

			// Создаём файл с текущей датой и временем
			var logFileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

			// Записываем сообщения в файл
			await File.WriteAllLinesAsync(logFileName, messages.Select(m => $"{m.Item1:yyyy-MM-dd HH:mm:ss.fff} - {m.Item2}"));

			Console.WriteLine($"Сообщения записаны в файл: {logFileName}");
		}
	}
}
