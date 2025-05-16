using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class MessageStore
{
	private ConcurrentBag<(DateTime timestamp, string message)> messageBag = new();
	private readonly object locker = new();

	// Добавить новое сообщение с текущим временем
	public void Add(string message)
	{
		messageBag.Add((DateTime.Now, message));
	}

	// Получить все сообщения и очистить хранилище
	public List<(DateTime, string)> GetAndClear()
	{
		lock (locker)
		{
			var list = messageBag.ToList();
			messageBag = new ConcurrentBag<(DateTime, string)>();
			return list;
		}
	}
}
