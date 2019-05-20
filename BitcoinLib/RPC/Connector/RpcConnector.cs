// Copyright (c) 2014 - 2016 George Kimionis
// Copyright (c) 2018 Jim Walker
// See the accompanying file LICENSE for the Software License Aggrement

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using BitcoinLib.Auxiliary;
using BitcoinLib.ExceptionHandling.Rpc;
using BitcoinLib.RPC.RequestResponse;
using BitcoinLib.RPC.Specifications;
using BitcoinLib.Services.Coins.Base;
using Newtonsoft.Json;

namespace BitcoinLib.RPC.Connector
{
    public sealed class RpcConnector : IRpcConnector
    {
        private static object locker = new object();
        private static Dictionary<string, HttpClient> clients = new Dictionary<string, HttpClient>();
        private HttpClient client;
        private readonly ICoinService _coinService;

        public RpcConnector(ICoinService coinService)
        {
            _coinService = coinService;
            lock (locker)
            {
                if (!clients.ContainsKey(_coinService.Parameters.SelectedDaemonUrl))
                {
                    HttpClientHandler hch = new HttpClientHandler();
                    hch.Proxy = null;
                    hch.UseProxy = false;
                    client = new HttpClient(hch);
                    clients.Add(_coinService.Parameters.SelectedDaemonUrl, client);
                }
                else
                {
                    client = clients[_coinService.Parameters.SelectedDaemonUrl];
                }
            }
            var sb = new StringBuilder();
            sb.Append(_coinService.Parameters.RpcUsername);
            sb.Append(":");
            sb.Append(_coinService.Parameters.RpcPassword);
            var authenticationBytes = Encoding.ASCII.GetBytes(sb.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(authenticationBytes));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private string MakeRawRequestClient(string rpcMethod, params object[] parameters)
        {
            var jsonRpcRequest = new JsonRpcRequest(1, rpcMethod, parameters);
            try
            {
                var request_json = jsonRpcRequest.GetJson();
                var content = new StringContent(request_json, Encoding.UTF8, "application/json");
                var result = client.PostAsync(_coinService.Parameters.SelectedDaemonUrl, content).Result;
                var result_string = result.Content.ReadAsStringAsync().Result;
                var resp = JsonConvert.DeserializeObject<JsonRpcResponse<object>>(result_string);
                return resp.Result.ToString();

            }
            catch (AggregateException aggregate)
            {
                foreach (var e in aggregate.Flatten().InnerExceptions)
                {
                    if (e is HttpRequestException)
                    {
                        throw new ConnectionException(e.Message, 1);
                    }
                    else
                    {
                        throw;
                    }
                }
                throw aggregate;
            }
            catch (JsonException jsonException)
            {
                throw new RpcResponseDeserializationException("There was a problem deserializing the response from the wallet", jsonException);
            }
            catch (ProtocolViolationException protocolViolationException)
            {
                throw new RpcException("Unable to connect to the server", protocolViolationException);
            }
            catch (Exception exception)
            {
                var queryParameters = jsonRpcRequest.Parameters.Cast<string>().Aggregate(string.Empty, (current, parameter) => current + (parameter + " "));
                throw new Exception($"A problem was encountered while calling MakeRpcRequest() for: {jsonRpcRequest.Method} with parameters: {queryParameters}. \nException: {exception.Message}");
            }
        }

        public string MakeRawRequest(string rpcMethod, params object[] parameters)
        {
            if (_coinService.Parameters.IsClient)
            {
                return MakeRawRequestClient(rpcMethod, parameters);
            }
            var jsonRpcRequest = new JsonRpcRequest(1, rpcMethod, parameters);
            var webRequest = (HttpWebRequest)WebRequest.Create(_coinService.Parameters.SelectedDaemonUrl);
            SetBasicAuthHeader(webRequest, _coinService.Parameters.RpcUsername, _coinService.Parameters.RpcPassword);
            webRequest.Credentials = new NetworkCredential(_coinService.Parameters.RpcUsername, _coinService.Parameters.RpcPassword);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";
            webRequest.Proxy = null;
            webRequest.Timeout = _coinService.Parameters.RpcRequestTimeoutInSeconds * GlobalConstants.MillisecondsInASecond;
            var byteArray = jsonRpcRequest.GetBytes();
            webRequest.ContentLength = jsonRpcRequest.GetBytes().Length;

            try
            {
                using (var dataStream = webRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Dispose();
                }
            }
            catch (Exception exception)
            {
                throw new RpcException("There was a problem sending the request to the wallet", exception);
            }

            try
            {
                string json;

                using (var webResponse = webRequest.GetResponse())
                {
                    using (var stream = webResponse.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var result = reader.ReadToEnd();
                            reader.Dispose();
                            json = result;
                        }
                    }
                    webResponse.Close();
                }
                var rpcResponse = JsonConvert.DeserializeObject<JsonRpcResponse<object>>(json);
                return rpcResponse.Result.ToString();
            }
            catch (WebException webException)
            {
                if (webException.InnerException != null
                    && webException.InnerException.GetType() == typeof(HttpRequestException)
                    && webException.InnerException.InnerException != null
                    && webException.InnerException.InnerException.Message.Contains("A connection with the server could not be established"))
                {
                    throw new ConnectionException(webException.InnerException.Message, 1);
                }
                #region RPC Internal Server Error (with an Error Code)

                var webResponse = webException.Response as HttpWebResponse;

                if (webResponse != null)
                {
                    switch (webResponse.StatusCode)
                    {
                        case HttpStatusCode.InternalServerError:
                            {
                                using (var stream = webResponse.GetResponseStream())
                                {
                                    if (stream == null)
                                    {
                                        throw new RpcException("The RPC request was either not understood by the server or there was a problem executing the request", webException);
                                    }

                                    using (var reader = new StreamReader(stream))
                                    {
                                        var result = reader.ReadToEnd();
                                        reader.Dispose();

                                        try
                                        {
                                            var jsonRpcResponseObject = JsonConvert.DeserializeObject<JsonRpcResponse<object>>(result);

                                            var internalServerErrorException = new RpcInternalServerErrorException(jsonRpcResponseObject.Error.Message, webException)
                                            {
                                                RpcErrorCode = jsonRpcResponseObject.Error.Code
                                            };

                                            throw internalServerErrorException;
                                        }
                                        catch (JsonException)
                                        {
                                            throw new RpcException(result, webException);
                                        }
                                    }
                                }
                            }

                        default:
                            throw new RpcException("The RPC request was either not understood by the server or there was a problem executing the request", webException);
                    }
                }

                #endregion

                #region RPC Time-Out

                if (webException.Message == "The operation has timed out")
                {
                    throw new RpcRequestTimeoutException(webException.Message);
                }

                #endregion

                throw new RpcException("An unknown web exception occured while trying to read the JSON response", webException);
            }
            catch (JsonException jsonException)
            {
                throw new RpcResponseDeserializationException("There was a problem deserializing the response from the wallet", jsonException);
            }
            catch (ProtocolViolationException protocolViolationException)
            {
                throw new RpcException("Unable to connect to the server", protocolViolationException);
            }
            catch (Exception exception)
            {
                var queryParameters = jsonRpcRequest.Parameters.Cast<string>().Aggregate(string.Empty, (current, parameter) => current + (parameter + " "));
                throw new Exception($"A problem was encountered while calling MakeRpcRequest() for: {jsonRpcRequest.Method} with parameters: {queryParameters}. \nException: {exception.Message}");
            }
        }

        private T MakeRequestClient<T>(RpcMethods rpcMethod, params object[] parameters)
        {
            var jsonRpcRequest = new JsonRpcRequest(1, rpcMethod.ToString(), parameters);
            try
            {
                var request_json = jsonRpcRequest.GetJson();
                var content = new StringContent(request_json, Encoding.UTF8, "application/json");
                var result = client.PostAsync(_coinService.Parameters.SelectedDaemonUrl, content).Result;
                var result_string = result.Content.ReadAsStringAsync().Result;
                var resp = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(result_string);
                return resp.Result;

            }
            catch (AggregateException aggregate)
            {
                foreach (var e in aggregate.Flatten().InnerExceptions)
                {
                    if (e is HttpRequestException)
                    {
                        throw new ConnectionException(e.Message, 1);
                    }
                    else
                    {
                        throw;
                    }
                }
                throw aggregate;
            }
            catch (JsonException jsonException)
            {
                throw new RpcResponseDeserializationException("There was a problem deserializing the response from the wallet", jsonException);
            }
            catch (ProtocolViolationException protocolViolationException)
            {
                throw new RpcException("Unable to connect to the server", protocolViolationException);
            }
            catch (Exception exception)
            {
                var queryParameters = jsonRpcRequest.Parameters.Cast<string>().Aggregate(string.Empty, (current, parameter) => current + (parameter + " "));
                throw new Exception($"A problem was encountered while calling MakeRpcRequest() for: {jsonRpcRequest.Method} with parameters: {queryParameters}. \nException: {exception.Message}");
            }
        }

        public T MakeRequest<T>(RpcMethods rpcMethod, params object[] parameters)
        {
            if (_coinService.Parameters.IsClient)
            {
                return MakeRequestClient<T>(rpcMethod, parameters);
            }
            var jsonRpcRequest = new JsonRpcRequest(1, rpcMethod.ToString(), parameters);
            var webRequest = (HttpWebRequest)WebRequest.Create(_coinService.Parameters.SelectedDaemonUrl);
            SetBasicAuthHeader(webRequest, _coinService.Parameters.RpcUsername, _coinService.Parameters.RpcPassword);
            webRequest.Credentials = new NetworkCredential(_coinService.Parameters.RpcUsername, _coinService.Parameters.RpcPassword);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";
            webRequest.Proxy = null;
            webRequest.Timeout = _coinService.Parameters.RpcRequestTimeoutInSeconds * GlobalConstants.MillisecondsInASecond;
            var byteArray = jsonRpcRequest.GetBytes();
            webRequest.ContentLength = jsonRpcRequest.GetBytes().Length;

            try
            {
                using (var dataStream = webRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Dispose();
                }
            }
            catch (Exception exception)
            {
                throw new RpcException("There was a problem sending the request to the wallet", exception);
            }

            try
            {
                string json;

                using (var webResponse = webRequest.GetResponse())
                {
                    using (var stream = webResponse.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var result = reader.ReadToEnd();
                            reader.Dispose();
                            json = result;
                        }
                    }
                    webResponse.Close();
                }
                var rpcResponse = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(json);
                return rpcResponse.Result;
            }
            catch (WebException webException)
            {
                if (webException.InnerException != null
                    && webException.InnerException.GetType() == typeof(HttpRequestException)
                    && webException.InnerException.InnerException != null
                    && webException.InnerException.InnerException.Message.Contains("A connection with the server could not be established"))
                {
                    throw new ConnectionException(webException.InnerException.Message, 1);
                }
                if (webException.InnerException != null
                    && webException.InnerException.GetType() == typeof(HttpRequestException)
                    && webException.InnerException.InnerException != null
                    && webException.InnerException.InnerException.Message.Contains("The operation timed out"))
                {
                    throw new ConnectionException(webException.InnerException.Message, 1);
                }
                #region RPC Internal Server Error (with an Error Code)

                var webResponse = webException.Response as HttpWebResponse;

                if (webResponse != null)
                {
                    switch (webResponse.StatusCode)
                    {
                        case HttpStatusCode.InternalServerError:
                            {
                                using (var stream = webResponse.GetResponseStream())
                                {
                                    if (stream == null)
                                    {
                                        throw new RpcException("The RPC request was either not understood by the server or there was a problem executing the request", webException);
                                    }

                                    using (var reader = new StreamReader(stream))
                                    {
                                        var result = reader.ReadToEnd();
                                        reader.Dispose();

                                        try
                                        {
                                            var jsonRpcResponseObject = JsonConvert.DeserializeObject<JsonRpcResponse<object>>(result);

                                            var internalServerErrorException = new RpcInternalServerErrorException(jsonRpcResponseObject.Error.Message, webException)
                                            {
                                                RpcErrorCode = jsonRpcResponseObject.Error.Code
                                            };

                                            throw internalServerErrorException;
                                        }
                                        catch (JsonException)
                                        {
                                            throw new RpcException(result, webException);
                                        }
                                    }
                                }
                            }

                        default:
                            throw new RpcException("The RPC request was either not understood by the server or there was a problem executing the request", webException);
                    }
                }

                #endregion

                #region RPC Time-Out

                if (webException.Message == "The operation has timed out")
                {
                    throw new RpcRequestTimeoutException(webException.Message);
                }

                #endregion

                throw new RpcException("An unknown web exception occured while trying to read the JSON response", webException);
            }
            catch (JsonException jsonException)
            {
                throw new RpcResponseDeserializationException("There was a problem deserializing the response from the wallet", jsonException);
            }
            catch (ProtocolViolationException protocolViolationException)
            {
                throw new RpcException("Unable to connect to the server", protocolViolationException);
            }
            catch (Exception exception)
            {
                var queryParameters = jsonRpcRequest.Parameters.Cast<string>().Aggregate(string.Empty, (current, parameter) => current + (parameter + " "));
                throw new Exception($"A problem was encountered while calling MakeRpcRequest() for: {jsonRpcRequest.Method} with parameters: {queryParameters}. \nException: {exception.Message}");
            }
        }

        private static void SetBasicAuthHeader(WebRequest webRequest, string username, string password)
        {
            var authInfo = username + ":" + password;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            webRequest.Headers["Authorization"] = "Basic " + authInfo;
        }
    }
}