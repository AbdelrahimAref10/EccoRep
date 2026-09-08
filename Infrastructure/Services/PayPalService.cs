using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class PayPalService : IPayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl;
        private readonly bool _isSandbox;
        private readonly string _returnUrl;
        private readonly string _cancelUrl;

        public PayPalService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _clientId = _configuration["PayPalSettings:ClientId"] ?? "";
            _clientSecret = _configuration["PayPalSettings:ClientSecret"] ?? "";
            _isSandbox = _configuration["PayPalSettings:UseSandbox"] == "true" || string.IsNullOrEmpty(_configuration["PayPalSettings:UseSandbox"]);
            _baseUrl = _isSandbox 
                ? "https://api.sandbox.paypal.com" 
                : "https://api.paypal.com";
            _returnUrl = _configuration["PayPalSettings:ReturnUrl"] ?? "voltapp://paypal/return";
            _cancelUrl = _configuration["PayPalSettings:CancelUrl"] ?? "voltapp://paypal/cancel";
        }

        public async Task<PayPalPaymentResult> ProcessPaymentAsync(string orderId, decimal amount, string currency = "EUR")
        {
            try
            {
                // For development/testing: If no PayPal credentials, simulate success
                if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
                {
                    Console.WriteLine($"PayPal Payment (Mock) for Order {orderId}:");
                    Console.WriteLine($"Amount: {amount} {currency}");
                    Console.WriteLine("Status: SUCCESS (Mock)");
                    await Task.Delay(500); // Simulate API call delay
                    return new PayPalPaymentResult
                    {
                        IsSuccess = true,
                        TransactionId = $"MOCK-{Guid.NewGuid()}"
                    };
                }

                // Get access token
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return new PayPalPaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to obtain PayPal access token"
                    };
                }

                // Create payment order
                var paymentResult = await CreatePaymentOrderAsync(accessToken, orderId, amount, currency);
                return paymentResult;
            }
            catch (Exception ex)
            {
                return new PayPalPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"PayPal payment error: {ex.Message}"
                };
            }
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/oauth2/token");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Accept-Language", "en_US");

                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
                request.Headers.Add("Authorization", $"Basic {credentials}");

                request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                return tokenResponse.GetProperty("access_token").GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting PayPal access token: {ex.Message}");
                return null;
            }
        }

        private async Task<PayPalPaymentResult> CreatePaymentOrderAsync(string accessToken, string orderId, decimal amount, string currency)
        {
            try
            {
                var requestBody = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            reference_id = orderId,
                            amount = new
                            {
                                currency_code = currency,
                                value = amount.ToString("F2")
                            }
                        }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");

                var jsonBody = JsonSerializer.Serialize(requestBody);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var orderIdFromPayPal = orderResponse.GetProperty("id").GetString();
                    
                    // Capture the payment
                    var captureResult = await CapturePaymentAsync(accessToken, orderIdFromPayPal);
                    return captureResult;
                }
                else
                {
                    return new PayPalPaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"PayPal API error: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PayPalPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Error creating PayPal order: {ex.Message}"
                };
            }
        }

        private async Task<PayPalPaymentResult> CapturePaymentAsync(string accessToken, string paypalOrderId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders/{paypalOrderId}/capture");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");
                
                // PayPal requires Content-Type header even with empty body
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var captureResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var status = captureResponse.GetProperty("status").GetString();
                    
                    if (status == "COMPLETED")
                    {
                        var transactionId = captureResponse
                            .GetProperty("purchase_units")[0]
                            .GetProperty("payments")
                            .GetProperty("captures")[0]
                            .GetProperty("id")
                            .GetString();

                        return new PayPalPaymentResult
                        {
                            IsSuccess = true,
                            TransactionId = transactionId
                        };
                    }
                    else
                    {
                        return new PayPalPaymentResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"PayPal payment status: {status}"
                        };
                    }
                }
                else
                {
                    return new PayPalPaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"PayPal capture error: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PayPalPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Error capturing PayPal payment: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Creates a PayPal order and returns the approve link for mobile app
        /// </summary>
        public async Task<PayPalCreateOrderResult> CreatePayPalOrderAsync(string orderId, decimal amount, string currency = "EUR")
        {
            try
            {
                // For development/testing: If no PayPal credentials, simulate success
                if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
                {
                    Console.WriteLine($"PayPal Create Order (Mock) for Order {orderId}:");
                    Console.WriteLine($"Amount: {amount} {currency}");
                    var mockPayPalOrderId = $"MOCK-{Guid.NewGuid()}";
                    var mockApproveLink = _isSandbox 
                        ? $"https://www.sandbox.paypal.com/checkoutnow?token={mockPayPalOrderId}"
                        : $"https://www.paypal.com/checkoutnow?token={mockPayPalOrderId}";
                    
                    return new PayPalCreateOrderResult
                    {
                        IsSuccess = true,
                        PayPalOrderId = mockPayPalOrderId,
                        ApproveLink = mockApproveLink
                    };
                }

                // Get access token
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return new PayPalCreateOrderResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to obtain PayPal access token"
                    };
                }

                // Create PayPal order
                var requestBody = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            reference_id = orderId,
                            amount = new
                            {
                                currency_code = currency,
                                value = amount.ToString("F2")
                            }
                        }
                    },
                    application_context = new
                    {
                        return_url = _returnUrl,
                        cancel_url = _cancelUrl
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");

                var jsonBody = JsonSerializer.Serialize(requestBody);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var paypalOrderId = orderResponse.GetProperty("id").GetString();
                    
                    // Extract approve link from links array
                    string? approveLink = null;
                    if (orderResponse.TryGetProperty("links", out var links))
                    {
                        foreach (var link in links.EnumerateArray())
                        {
                            if (link.TryGetProperty("rel", out var rel) && rel.GetString() == "approve")
                            {
                                approveLink = link.GetProperty("href").GetString();
                                break;
                            }
                        }
                    }

                    return new PayPalCreateOrderResult
                    {
                        IsSuccess = true,
                        PayPalOrderId = paypalOrderId,
                        ApproveLink = approveLink
                    };
                }
                else
                {
                    return new PayPalCreateOrderResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"PayPal API error: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PayPalCreateOrderResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Error creating PayPal order: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Captures a PayPal order after customer approval (called by mobile app)
        /// </summary>
        public async Task<PayPalCaptureResult> CapturePayPalOrderAsync(string paypalOrderId)
        {
            try
            {
                // For development/testing: If no PayPal credentials, simulate success
                if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
                {
                    Console.WriteLine($"PayPal Capture Order (Mock) for PayPal Order {paypalOrderId}:");
                    Console.WriteLine("Status: COMPLETED (Mock)");
                    await Task.Delay(500); // Simulate API call delay
                    
                    return new PayPalCaptureResult
                    {
                        IsSuccess = true,
                        PayPalOrderId = paypalOrderId,
                        TransactionId = $"MOCK-CAPTURE-{Guid.NewGuid()}",
                        Status = "COMPLETED"
                    };
                }

                // Get access token
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return new PayPalCaptureResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to obtain PayPal access token"
                    };
                }

                // Check order status before capturing - order must be APPROVED
                var orderStatus = await GetPayPalOrderStatusAsync(accessToken, paypalOrderId);
                if (orderStatus == null)
                {
                    return new PayPalCaptureResult
                    {
                        IsSuccess = false,
                        PayPalOrderId = paypalOrderId,
                        ErrorMessage = "Failed to retrieve PayPal order status"
                    };
                }

                if (orderStatus != "APPROVED")
                {
                    return new PayPalCaptureResult
                    {
                        IsSuccess = false,
                        PayPalOrderId = paypalOrderId,
                        Status = orderStatus,
                        ErrorMessage = $"PayPal order is not approved. Current status: {orderStatus}. Customer must approve the order first."
                    };
                }

                // Capture the payment
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders/{paypalOrderId}/capture");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");
                
                // PayPal requires Content-Type header even with empty body
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var captureResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var status = captureResponse.GetProperty("status").GetString();
                    
                    if (status == "COMPLETED")
                    {
                        string? transactionId = null;
                        if (captureResponse.TryGetProperty("purchase_units", out var purchaseUnits) && purchaseUnits.GetArrayLength() > 0)
                        {
                            var firstUnit = purchaseUnits[0];
                            if (firstUnit.TryGetProperty("payments", out var payments))
                            {
                                if (payments.TryGetProperty("captures", out var captures) && captures.GetArrayLength() > 0)
                                {
                                    transactionId = captures[0].GetProperty("id").GetString();
                                }
                            }
                        }

                        return new PayPalCaptureResult
                        {
                            IsSuccess = true,
                            PayPalOrderId = paypalOrderId,
                            TransactionId = transactionId,
                            Status = status
                        };
                    }
                    else
                    {
                        return new PayPalCaptureResult
                        {
                            IsSuccess = false,
                            PayPalOrderId = paypalOrderId,
                            Status = status,
                            ErrorMessage = $"PayPal payment status: {status}"
                        };
                    }
                }
                else
                {
                    return new PayPalCaptureResult
                    {
                        IsSuccess = false,
                        PayPalOrderId = paypalOrderId,
                        ErrorMessage = $"PayPal capture error: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PayPalCaptureResult
                {
                    IsSuccess = false,
                    PayPalOrderId = paypalOrderId,
                    ErrorMessage = $"Error capturing PayPal payment: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets the status of a PayPal order
        /// </summary>
        private async Task<string?> GetPayPalOrderStatusAsync(string accessToken, string paypalOrderId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v2/checkout/orders/{paypalOrderId}");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    return orderResponse.GetProperty("status").GetString();
                }
                else
                {
                    Console.WriteLine($"Error getting PayPal order status: {responseContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting PayPal order status: {ex.Message}");
                return null;
            }
        }
    }
}
