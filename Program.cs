using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;

using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

var builder = WebApplication.CreateBuilder(args);

// ������������ ������ ��� �������� ���������
builder.Services.AddSingleton<MessageStore>();

// ������������ ������� ������ ��� ����������� ���������
builder.Services.AddHostedService<LogMessagesBackgroundService>();

// ��������� ������� ��� ������������, Swagger � ��������
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

string serverVersion = "1.0.4";
string awsSecretKeyDB = "/server-api/awsSecretKeyDB";
string awsRegion = Environment.GetEnvironmentVariable("Region");

// ������� ������ AWS SSM � �������� ������ (���� �����)
var ssm = new Amazon.SimpleSystemsManagement.AmazonSimpleSystemsManagementClient();
var secretResponse = await ssm.GetParameterAsync(new Amazon.SimpleSystemsManagement.Model.GetParameterRequest
{
	Name = awsSecretKeyDB,
	WithDecryption = true
});

var app = builder.Build();

app.MapHealthChecks("/healthy");

// Swagger UI � ������ ����������
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Endpoint /check - ���������� ��� ������� � IP
app.MapGet("/check", (HttpContext context) =>
{
	string serverName = Environment.MachineName;
	var connectionFeature = context.Features.Get<IHttpConnectionFeature>();
	string serverIp = connectionFeature?.LocalIpAddress?.ToString() ?? "unknown";

	return $"Check = Ok, \nServerVersion = {serverVersion}, \nServerName = {serverName}, \nServerIP = {serverIp}, \nRegion = {awsRegion}, \nAWS Secret = {secretResponse.Parameter.Value}";
});

// Endpoint /receive-message - ��������� POST � ����������
app.MapPost("/receive-message", async (HttpContext context, MessageStore store) =>
{
	using var reader = new StreamReader(context.Request.Body);
	string message = await reader.ReadToEndAsync();

	store.Add(message);

	DateTime messageReceivedTime = DateTime.Now;
	DateTime responseSendTime = DateTime.Now;
	TimeSpan processingTime = responseSendTime - messageReceivedTime;
	string serverIp = context.Connection.LocalIpAddress?.ToString() ?? "unknown";

	Console.WriteLine($"Message received: {messageReceivedTime:yyyy-MM-dd HH:mm:ss.fff} - {message}");

	var response = new
	{
		MessageReceivedTime = messageReceivedTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
		ResponseSendTime = responseSendTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
		ProcessingTimeMs = processingTime.TotalMilliseconds,
		ServerIp = serverIp
	};

	context.Response.ContentType = "application/json";
	await context.Response.WriteAsync(JsonSerializer.Serialize(response));
});

app.Run();