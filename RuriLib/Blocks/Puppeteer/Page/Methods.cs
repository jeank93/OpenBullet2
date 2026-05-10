using PuppeteerSharp;
using RuriLib.Attributes;
using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Puppeteer.Page;

/// <summary>
/// Blocks for interacting with a puppeteer browser page.
/// </summary>
[BlockCategory("Page", "Blocks for interacting with a puppeteer browser page", "#e9967a")]
public static class Methods
{
    /// <summary>
    /// Navigates to a given URL in the current page.
    /// </summary>
    [Block("Navigates to a given URL in the current page", name = "Navigate To")]
    public static async Task PuppeteerNavigateTo(BotData data, string url = "https://example.com",
        WaitUntilNavigation loadedEvent = WaitUntilNavigation.Load, string referer = "", int timeout = 30000)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        var options = new NavigationOptions
        {
            Timeout = timeout,
            Referer = referer,
            WaitUntil = [loadedEvent]
        };
        var response = await page.GoToAsync(url, options);
        data.ADDRESS = response?.Url ?? page.Url;

        if (response is not null)
        {
            data.SOURCE = await response.TextAsync();
            data.RAWSOURCE = await response.BufferAsync();
        }
        else
        {
            data.SOURCE = await page.GetContentAsync();
            data.RAWSOURCE = Encoding.UTF8.GetBytes(data.SOURCE);
        }

        SwitchToMainFramePrivate(data);

        data.Logger.Log($"Navigated to {url}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Waits for navigation to complete.
    /// </summary>
    [Block("Waits for navigation to complete", name = "Wait for Navigation")]
    public static async Task PuppeteerWaitForNavigation(BotData data,
        WaitUntilNavigation loadedEvent = WaitUntilNavigation.Load, int timeout = 30000)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        var options = new NavigationOptions
        {
            Timeout = timeout,
            WaitUntil = [loadedEvent]
        };

        await page.WaitForNavigationAsync(options);
        data.ADDRESS = page.Url;
        data.SOURCE = await page.GetContentAsync();
        data.RAWSOURCE = Encoding.UTF8.GetBytes(data.SOURCE);
        SwitchToMainFramePrivate(data);

        data.Logger.Log("Waited for navigation to complete", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Clears cookies in the page stored for a specific website.
    /// </summary>
    [Block("Clears cookies in the page stored for a specific website", name = "Clear Cookies")]
    public static async Task PuppeteerClearCookies(BotData data, string website)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        var cookies = await page.GetCookiesAsync(website);
        await page.DeleteCookieAsync(cookies);
        data.Logger.Log($"Cookies cleared for site {website}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Sends keystrokes to the browser page.
    /// </summary>
    [Block("Sends keystrokes to the browser page", name = "Type in Page")]
    public static async Task PuppeteerPageType(BotData data, string text)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        await page.Keyboard.TypeAsync(text);
        data.Logger.Log($"Typed {text}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Presses and releases a key in the browser page.
    /// </summary>
    [Block("Presses and releases a key in the browser page", name = "Key Press in Page",
        extraInfo = "Full list of keys here: https://github.com/puppeteer/puppeteer/blob/v1.14.0/lib/USKeyboardLayout.js")]
    public static async Task PuppeteerPageKeyPress(BotData data, string key)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        await page.Keyboard.PressAsync(key);
        data.Logger.Log($"Pressed and released {key}", LogColors.DarkSalmon);

        // Full list of keys: https://github.com/puppeteer/puppeteer/blob/v1.14.0/lib/USKeyboardLayout.js
    }

    /// <summary>
    /// Clicks the page at the given coordinates.
    /// </summary>
    [Block("Clicks the page at the given coordinates", name = "Click at Coordinates")]
    public static async Task PuppeteerClickAtCoordinates(BotData data, int x, int y,
        PuppeteerSharp.Input.MouseButton mouseButton = PuppeteerSharp.Input.MouseButton.Left, int clickCount = 1,
        int timeBetweenClicks = 0)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        var frame = GetFrame(data);
        var (clickX, clickY) = await ResolveClickPoint(frame, x, y);
        await page.Mouse.ClickAsync(clickX, clickY, new PuppeteerSharp.Input.ClickOptions
        {
            Button = mouseButton,
            Count = clickCount,
            Delay = timeBetweenClicks
        });

        data.Logger.Log($"Clicked {clickCount} time(s) with {mouseButton} button at ({x}, {y})", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Presses a key in the browser page without releasing it.
    /// </summary>
    [Block("Presses a key in the browser page without releasing it", name = "Key Down in Page",
        extraInfo = "Full list of keys here: https://github.com/puppeteer/puppeteer/blob/v1.14.0/lib/USKeyboardLayout.js")]
    public static async Task PuppeteerPageKeyDown(BotData data, string key)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        await page.Keyboard.DownAsync(key);
        data.Logger.Log($"Pressed (and holding down) {key}", LogColors.DarkSalmon);

        // Full list of keys: https://github.com/puppeteer/puppeteer/blob/v1.14.0/lib/USKeyboardLayout.js
    }

    /// <summary>
    /// Releases a key that was previously pressed in the browser page.
    /// </summary>
    [Block("Releases a key that was previously pressed in the browser page", name = "Key Up in Page",
        extraInfo = "Full list of keys here: https://github.com/puppeteer/puppeteer/blob/v1.14.0/lib/USKeyboardLayout.js")]
    public static async Task PuppeteerKeyUp(BotData data, string key)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        await page.Keyboard.UpAsync(key);
        data.Logger.Log($"Released {key}", LogColors.DarkSalmon);

        // Full list of keys: https://github.com/puppeteer/puppeteer/blob/v1.14.0/lib/USKeyboardLayout.js
    }

    /// <summary>
    /// Takes a screenshot of the entire browser page and saves it to an output file.
    /// </summary>
    [Block("Takes a screenshot of the entire browser page and saves it to an output file", name = "Screenshot Page")]
    public static async Task PuppeteerScreenshotPage(BotData data, string file, bool fullPage = false, bool omitBackground = false)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        var options = new ScreenshotOptions
        {
            FullPage = fullPage,
            OmitBackground = omitBackground,
            Type = omitBackground ? ScreenshotType.Png : ScreenshotType.Jpeg,
            Quality = omitBackground ? null : 100
        };
        await page.ScreenshotAsync(file, options);
        data.Logger.Log($"Took a screenshot of the {(fullPage ? "full" : "visible")} page and saved it to {file}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Takes a screenshot of the entire browser page and converts it to a base64 string.
    /// </summary>
    [Block("Takes a screenshot of the entire browser page and converts it to a base64 string", name = "Screenshot Page Base64")]
    public static async Task<string> PuppeteerScreenshotPageBase64(BotData data, bool fullPage = false, bool omitBackground = false)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        var options = new ScreenshotOptions
        {
            FullPage = fullPage,
            OmitBackground = omitBackground,
            Type = omitBackground ? ScreenshotType.Png : ScreenshotType.Jpeg,
            Quality = omitBackground ? null : 100
        };
        var base64 = await page.ScreenshotBase64Async(options);
        data.Logger.Log($"Took a screenshot of the {(fullPage ? "full" : "visible")} page as base64", LogColors.DarkSalmon);
        return base64;
    }

    /// <summary>
    /// Scrolls to the top of the page.
    /// </summary>
    [Block("Scrolls to the top of the page", name = "Scroll to Top")]
    public static async Task PuppeteerScrollToTop(BotData data)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        await page.EvaluateExpressionAsync("window.scrollTo(0, 0);");
        data.Logger.Log("Scrolled to the top of the page", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Scrolls to the bottom of the page.
    /// </summary>
    [Block("Scrolls to the bottom of the page", name = "Scroll to Bottom")]
    public static async Task PuppeteerScrollToBottom(BotData data)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight);");
        data.Logger.Log("Scrolled to the bottom of the page", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Scrolls the page by a certain amount horizontally and vertically.
    /// </summary>
    [Block("Scrolls the page by a certain amount horizontally and vertically", name = "Scroll by")]
    public static async Task PuppeteerScrollBy(BotData data, int horizontalScroll, int verticalScroll)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        await page.EvaluateExpressionAsync($"window.scrollBy({horizontalScroll}, {verticalScroll});");
        data.Logger.Log($"Scrolled by ({horizontalScroll}, {verticalScroll})", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Sets the viewport dimensions and options.
    /// </summary>
    [Block("Sets the viewport dimensions and options", name = "Set Viewport")]
    public static async Task PuppeteerSetViewport(BotData data, int width, int height, bool isMobile = false, bool isLandscape = false, float scaleFactor = 1f)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);

        var options = new ViewPortOptions
        {
            Width = width,
            Height = height,
            IsMobile = isMobile,
            IsLandscape = isLandscape,
            DeviceScaleFactor = scaleFactor
        };

        await page.SetViewportAsync(options);

        data.Logger.Log($"Set the viewport size to {width}x{height}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Gets the current URL of the page.
    /// </summary>
    [Block("Gets the current URL of the page", name = "Get Current URL")]
    public static string PuppeteerGetCurrentUrl(BotData data)
    {
        data.Logger.LogHeader();

        var currentUrl = GetPage(data).Url;
        data.ADDRESS = currentUrl;

        data.Logger.Log($"Current URL: {currentUrl}", LogColors.DarkSalmon);
        return currentUrl;
    }

    /// <summary>
    /// Gets the full DOM of the page.
    /// </summary>
    [Block("Gets the full DOM of the page", name = "Get DOM")]
    public static async Task<string> PuppeteerGetDOM(BotData data)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        var dom = await page.EvaluateExpressionAsync<string>("document.body.innerHTML");

        data.Logger.Log("Got the full page DOM", LogColors.DarkSalmon);
        data.Logger.Log(dom, LogColors.DarkSalmon, true);
        return dom;
    }

    /// <summary>
    /// Gets the cookies for a given domain from the browser. If the domain is empty, gets all cookies from the page..
    /// </summary>
    [Block("Gets the cookies for a given domain from the browser. If the domain is empty, gets all cookies from the page.", name = "Get Cookies")]
    public static async Task<Dictionary<string, string>> PuppeteerGetCookies(BotData data, string domain)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        var cookies = await page.GetCookiesAsync();

        if (!string.IsNullOrWhiteSpace(domain))
        {
            cookies = cookies.Where(c => c.Domain.Contains(domain, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        data.Logger.Log($"Got {cookies.Length} cookies for {(string.IsNullOrWhiteSpace(domain) ? "all domains" : domain)}", LogColors.DarkSalmon);
        return cookies.ToDictionary(c => c.Name, c => c.Value);
    }

    /// <summary>
    /// Sets the cookies for a given domain in the browser page.
    /// </summary>
    [Block("Sets the cookies for a given domain in the browser page", name = "Set Cookies")]
    public static async Task PuppeteerSetCookies(BotData data, string domain, Dictionary<string, string> cookies)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        await page.SetCookieAsync(cookies.Select(c => new CookieParam { Domain = domain, Name = c.Key, Value = c.Value }).ToArray());

        data.Logger.Log($"Set {cookies.Count} cookies for {domain}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Sets the User Agent of the browser page.
    /// </summary>
    [Block("Sets the User Agent of the browser page", name = "Set User-Agent")]
    public static async Task PuppeteerSetUserAgent(BotData data, string userAgent)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        await page.SetUserAgentAsync(new SetUserAgentOptions
        {
            UserAgent = userAgent
        });

        data.Logger.Log($"User Agent set to {userAgent}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Switches to the main frame of the page.
    /// </summary>
    [Block("Switches to the main frame of the page", name = "Switch to Main Frame")]
    public static void PuppeteerSwitchToMainFrame(BotData data)
    {
        data.Logger.LogHeader();

        SwitchToMainFramePrivate(data);
        data.Logger.Log("Switched to main frame", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Evaluates a js expression in the current page and returns a json response.
    /// </summary>
    [Block("Evaluates a js expression in the current page and returns a json response", name = "Execute JS")]
    public static async Task<string> PuppeteerExecuteJs(BotData data, [MultiLine] string expression)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        await using var response = await frame.EvaluateExpressionHandleAsync(expression);
        var value = await response.JsonValueAsync<object>();
        var json = SerializeJavaScriptResult(value);
        data.Logger.Log($"Evaluated {expression}", LogColors.DarkSalmon);
        data.Logger.Log($"Got result: {json}", LogColors.DarkSalmon);

        return json;
    }

    /// <summary>
    /// Captures the response from the given URL.
    /// </summary>
    [Block("Captures the response from the given URL", name = "Wait for Response")]
    public static async Task PuppeteerWaitForResponse(BotData data, string url, int timeoutMilliseconds = 60000)
    {
        data.Logger.LogHeader();

        var page = GetPage(data);
        var options = new WaitForOptions
        {
            Timeout = timeoutMilliseconds
        };

        var response = await page.WaitForResponseAsync(r => UrlsMatch(r.Url, url), options);

        data.ADDRESS = response.Url;
        data.RESPONSECODE = (int)response.Status;
        data.HEADERS = response.Headers;
        data.SOURCE = string.Empty;
        data.RAWSOURCE = [];

        if (ResponseCanHaveBody(response))
        {
            await TryPopulateResponseBody(data, response);
        }

        data.Logger.Log($"Address: {data.ADDRESS}", LogColors.DodgerBlue);
        data.Logger.Log($"Response code: {data.RESPONSECODE}", LogColors.Citrine);

        data.Logger.Log("Received Headers:", LogColors.MediumPurple);
        data.Logger.Log(data.HEADERS.Select(h => $"{h.Key}: {h.Value}"), LogColors.Violet);

        data.Logger.Log("Received Payload:", LogColors.ForestGreen);
        data.Logger.Log(data.SOURCE, LogColors.GreenYellow, true);
    }

    private static IPage GetPage(BotData data)
        => data.TryGetObject<IPage>("puppeteerPage") ?? throw new BlockExecutionException("No pages open!");

    private static IFrame GetFrame(BotData data)
        => data.TryGetObject<IFrame>("puppeteerFrame") ?? GetPage(data).MainFrame;

    private static void SwitchToMainFramePrivate(BotData data)
        => data.SetObject("puppeteerFrame", GetPage(data).MainFrame);

    private static async Task<(decimal X, decimal Y)> ResolveClickPoint(IFrame frame, int x, int y)
    {
        if (frame.ParentFrame is null)
        {
            return (x, y);
        }

        var frameElement = await frame.FrameElementAsync();
        var frameBounds = await frameElement.BoundingBoxAsync()
            ?? throw new BlockExecutionException("Could not determine the current frame bounds");
        var frameClientLeft = await frameElement.EvaluateFunctionAsync<decimal>("element => element.clientLeft || 0");
        var frameClientTop = await frameElement.EvaluateFunctionAsync<decimal>("element => element.clientTop || 0");

        return (frameBounds.X + frameClientLeft + x, frameBounds.Y + frameClientTop + y);
    }

    private static bool UrlsMatch(string actual, string expected)
    {
        if (!Uri.TryCreate(actual, UriKind.Absolute, out var actualUri) ||
            !Uri.TryCreate(expected, UriKind.Absolute, out var expectedUri))
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(actualUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.Unescaped),
                             expectedUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.Unescaped),
                             StringComparison.OrdinalIgnoreCase);
    }

    private static bool ResponseCanHaveBody(IResponse response)
    {
        var statusCode = (int)response.Status;
        return statusCode is not (>= 100 and < 200 or 204 or 205 or 304) &&
               statusCode / 100 != 3;
    }

    private static async Task TryPopulateResponseBody(BotData data, IResponse response)
    {
        try
        {
            data.SOURCE = await response.TextAsync();
            data.RAWSOURCE = await response.BufferAsync();
        }
        catch (Exception ex) when (IsMissingResponseBodyException(ex))
        {
            data.SOURCE = string.Empty;
            data.RAWSOURCE = [];
            data.Logger.Log("Response body is not available", LogColors.Orange);
        }
    }

    private static bool IsMissingResponseBodyException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("Unable to get response body", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string SerializeJavaScriptResult(object value)
        => value switch
        {
            null => "undefined",
            JsonElement jsonElement => SerializeJsonElement(jsonElement),
            string stringValue => stringValue,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => JsonConvert.SerializeObject(value)
        };

    private static string SerializeJsonElement(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.Undefined or JsonValueKind.Null => "undefined",
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True or JsonValueKind.False => value.GetBoolean().ToString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => value.GetRawText()
        };
}
