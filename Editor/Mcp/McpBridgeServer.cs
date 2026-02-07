using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.Environment;
using GPTUnity.Actions;
using GPTUnity.Actions.Interfaces;
using GPTUnity.Api;
using GPTUnity.Data;
using GPTUnity.Helpers;
using GPTUnity.Indexing;
using GPTUnity.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mcp
{
    public static class McpBridgeServer
    {
        private static HttpListener _listener;
        private static CancellationTokenSource _cancellation;
        private static Task _listenTask;
        private static GptTypesRegister _typesRegister;
        private static GptActionsFactory _actionsFactory;

        public static bool IsRunning => _listener != null && _listener.IsListening;

        public static void Start(string url)
        {
            if (IsRunning)
                return;

            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogError("[MCP] MCP Bridge URL is not set.");
                return;
            }

            var prefix = NormalizePrefix(url);
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);

            try
            {
                _listener.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP] Failed to start MCP Bridge at {prefix}: {e.Message}");
                _listener = null;
                return;
            }

            _typesRegister = new GptTypesRegister(typeof(GPTAssistantAction));
            _actionsFactory = new GptActionsFactory();
            _actionsFactory.Init(_typesRegister);

            _cancellation = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cancellation.Token));
            Debug.Log($"[MCP] MCP Bridge started at {prefix}");
        }

        public static void Stop()
        {
            if (!IsRunning)
                return;

            try
            {
                _cancellation?.Cancel();
                _listener?.Stop();
                _listener?.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP] Failed to stop MCP Bridge: {e.Message}");
            }
            finally
            {
                _listener = null;
                _cancellation = null;
                _listenTask = null;
            }
        }

        private static async Task ListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    if (_listener == null || !_listener.IsListening)
                        break;
                }

                if (context != null)
                {
                    _ = Task.Run(() => HandleContextAsync(context), token);
                }
            }
        }

        private static async Task HandleContextAsync(HttpListenerContext context)
        {
            try
            {
                var path = context.Request.Url.AbsolutePath.TrimEnd('/');
                if (path.EndsWith("/health"))
                {
                    await WriteJsonAsync(context.Response, new { ok = true });
                    return;
                }

                if (path.EndsWith("/tools") && context.Request.HttpMethod == "GET")
                {
                    var tools = McpToolRegistry.GetTools();
                    await WriteJsonAsync(context.Response, tools);
                    return;
                }

                if (path.EndsWith("/tools/call") && context.Request.HttpMethod == "POST")
                {
                    var body = await ReadBodyAsync(context.Request);
                    var request = ParseToolCallRequest(body);
                    if (request == null)
                    {
                        context.Response.StatusCode = 400;
                        await WriteJsonAsync(context.Response, new McpToolCallResponse
                        {
                            ok = false,
                            content = "Invalid request body.",
                            isError = true
                        });
                        return;
                    }

                    var response = await ExecuteToolAsync(request);
                    await WriteJsonAsync(context.Response, response);
                    return;
                }

                context.Response.StatusCode = 404;
                await WriteJsonAsync(context.Response, new { ok = false, message = "Not found." });
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;
                await WriteJsonAsync(context.Response, new { ok = false, message = e.Message });
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }

        private static async Task<McpToolCallResponse> ExecuteToolAsync(McpToolCallRequest request)
        {
            try
            {
                return await EditorMainThread.RunAsync(async () =>
                {
                    var args = ToStringDictionary(request.arguments);
                    var functionCall = new GPTFunctionCall
                    {
                        name = request.name,
                        arguments = JsonConvert.SerializeObject(args)
                    };

                    var action = _actionsFactory.CreateActionFromFunctionCall(functionCall);
                    if (action == null)
                    {
                        return new McpToolCallResponse
                        {
                            ok = false,
                            content = $"Action not found: {request.name}",
                            isError = true
                        };
                    }

                    if (action is IGPTActionThatRequiresIndexingApi actionThatRequiresIndexingApi)
                    {
                        actionThatRequiresIndexingApi.Indexing =
                            new DeepSearchClient(ChatSettings.instance.SearchApiHost, ChatSettings.instance.SearchApiPythonPathResolved);
                    }

                    if (action is IGPTActionThatRequiresImagesApi actionThatRequiresImagesApi)
                    {
                        if (!Env.TryGetEnv("OPENAI_API_KEY", out var apiKey))
                        {
                            return new McpToolCallResponse
                            {
                                ok = false,
                                content = "OPENAI_API_KEY environment variable is not set.",
                                isError = true
                            };
                        }

                        actionThatRequiresImagesApi.Images = new OpenAIImageServiceApi(key: apiKey);
                    }

                    var result = await action.Execute();
                    return new McpToolCallResponse
                    {
                        ok = true,
                        content = result,
                        isError = false
                    };
                });
            }
            catch (Exception e)
            {
                return new McpToolCallResponse
                {
                    ok = false,
                    content = e.Message,
                    isError = true
                };
            }
        }

        private static McpToolCallRequest ParseToolCallRequest(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                var json = JObject.Parse(body);
                var name = json["name"]?.ToString();
                var argsToken = json["arguments"] as JObject;
                var args = argsToken != null
                    ? argsToken.ToObject<Dictionary<string, object>>()
                    : new Dictionary<string, object>();

                return new McpToolCallRequest
                {
                    name = name,
                    arguments = args
                };
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, string> ToStringDictionary(Dictionary<string, object> input)
        {
            var output = new Dictionary<string, string>();
            if (input == null)
                return output;

            foreach (var kvp in input)
            {
                output[kvp.Key] = kvp.Value?.ToString();
            }

            return output;
        }

        private static string NormalizePrefix(string url)
        {
            var prefix = url.Trim();
            if (!prefix.EndsWith("/"))
                prefix += "/";
            return prefix;
        }

        private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private static async Task WriteJsonAsync(HttpListenerResponse response, object payload)
        {
            response.ContentType = "application/json";
            var json = JsonConvert.SerializeObject(payload);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}
