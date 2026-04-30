using PuppeteerSharp;
using RuriLib.Attributes;
using RuriLib.Exceptions;
using RuriLib.Functions.Files;
using RuriLib.Functions.Puppeteer;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PuppeteerSharp.Media;

namespace RuriLib.Blocks.Puppeteer.Elements;

/// <summary>
/// Blocks for interacting with elements in a Puppeteer browser page.
/// </summary>
[BlockCategory("Elements", "Blocks for interacting with elements on a puppeteer browser page", "#e9967a")]
public static class Methods
{
    /// <summary>
    /// Sets the value of the specified attribute of an element.
    /// </summary>
    [Block("Sets the value of the specified attribute of an element", name = "Set Attribute Value")]
    public static async Task PuppeteerSetAttributeValue(BotData data, FindElementBy findBy, string identifier, int index,
        string attributeName, string value)
    {
        data.Logger.LogHeader();

        var elem = await GetElement(GetFrame(data), findBy, identifier, index);
        var script = "(element, attributeName, value) => element.setAttribute(attributeName, value)";
        await elem.EvaluateFunctionAsync(script, attributeName, value);

        data.Logger.Log($"Set value {value} of attribute {attributeName} by executing {script}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Types text in an input field.
    /// </summary>
    [Block("Types text in an input field", name = "Type")]
    public static async Task PuppeteerTypeElement(BotData data, FindElementBy findBy, string identifier, int index,
        string text, int timeBetweenKeystrokes = 0)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        await elem.TypeAsync(text, new PuppeteerSharp.Input.TypeOptions { Delay = timeBetweenKeystrokes });

        data.Logger.Log($"Typed {text}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Types text in an input field with human-like random delays.
    /// </summary>
    [Block("Types text in an input field with human-like random delays", name = "Type Human")]
    public static async Task PuppeteerTypeElementHuman(BotData data, FindElementBy findBy, string identifier, int index,
        string text)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);

        foreach (var c in text)
        {
            await elem.TypeAsync(c.ToString());
            await Task.Delay(data.Random.Next(100, 300), data.CancellationToken); // Wait between 100 and 300 ms (average human type speed is 60 WPM ~ 360 CPM)
        }

        data.Logger.Log($"Typed {text}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Clicks an element.
    /// </summary>
    [Block("Clicks an element", name = "Click")]
    public static async Task PuppeteerClick(BotData data, FindElementBy findBy, string identifier, int index,
        PuppeteerSharp.Input.MouseButton mouseButton = PuppeteerSharp.Input.MouseButton.Left, int clickCount = 1,
        int timeBetweenClicks = 0)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        await elem.ClickAsync(new PuppeteerSharp.Input.ClickOptions { Button = mouseButton, Count = clickCount, Delay = timeBetweenClicks });

        data.Logger.Log($"Clicked {clickCount} time(s) with {mouseButton} button", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Submits a form.
    /// </summary>
    [Block("Submits a form", name = "Submit")]
    public static async Task PuppeteerSubmit(BotData data, FindElementBy findBy, string identifier, int index)
    {
        data.Logger.LogHeader();

        var elem = await GetElement(GetFrame(data), findBy, identifier, index);
        var script = """
                     element => {
                         const form = element.tagName === 'FORM' ? element : element.form;
                         if (!form) {
                             throw new Error('The selected element is not associated with a form');
                         }

                         if (typeof form.requestSubmit === 'function') {
                             form.requestSubmit();
                             return;
                         }

                         const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
                         if (form.dispatchEvent(submitEvent)) {
                             form.submit();
                         }
                     }
                     """;
        await elem.EvaluateFunctionAsync(script);

        data.Logger.Log($"Submitted the form by executing {script}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Selects a value in a select element.
    /// </summary>
    [Block("Selects a value in a select element", name = "Select")]
    public static async Task PuppeteerSelect(BotData data, FindElementBy findBy, string identifier, int index, string value)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        await elem.SelectAsync(value);

        data.Logger.Log($"Selected value {value}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Selects a value by index in a select element.
    /// </summary>
    [Block("Selects a value by index in a select element", name = "Select by Index")]
    public static async Task PuppeteerSelectByIndex(BotData data, FindElementBy findBy, string identifier, int index, int selectionIndex)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        var value = await elem.EvaluateFunctionAsync<string>(
            """
            (element, selectedIndex) => {
                const option = element.getElementsByTagName('option')[selectedIndex];
                return option?.value ?? null;
            }
            """,
            selectionIndex);

        if (value is null)
        {
            throw new BlockExecutionException($"Expected an option at index {selectionIndex} but none was found");
        }

        await elem.SelectAsync(value);

        data.Logger.Log($"Selected value {value}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Selects a value by text in a select element.
    /// </summary>
    [Block("Selects a value by text in a select element", name = "Select by Text")]
    public static async Task PuppeteerSelectByText(BotData data, FindElementBy findBy, string identifier, int index, string text)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elemScript = GetElementScript(findBy, identifier, index);
        var script = $"el={elemScript};for(let i=0;i<el.options.length;i++){{if(el.options[i].text=={ToJavaScriptStringLiteral(text)}){{el.selectedIndex = i;break;}}}}";
        await frame.EvaluateExpressionAsync(script);

        data.Logger.Log($"Selected text {text}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Gets the value of an attribute of an element.
    /// </summary>
    [Block("Gets the value of an attribute of an element", name = "Get Attribute Value")]
    public static async Task<string> PuppeteerGetAttributeValue(BotData data, FindElementBy findBy, string identifier, int index,
        string attributeName = "innerText")
    {
        data.Logger.LogHeader();

        var elem = await GetElement(GetFrame(data), findBy, identifier, index);
        var script = """
                     (element, attributeName) => {
                         const value = attributeName
                             .split('.')
                             .reduce((current, part) => current?.[part], element);
                         return value?.toString() ?? '';
                     }
                     """;
        var value = await elem.EvaluateFunctionAsync<string>(script, attributeName);

        data.Logger.Log($"Got value {value} of attribute {attributeName} by executing {script}", LogColors.DarkSalmon);
        return value;
    }

    /// <summary>
    /// Gets the values of an attribute from multiple elements.
    /// </summary>
    [Block("Gets the values of an attribute of multiple elements", name = "Get Attribute Value All")]
    public static async Task<List<string>> PuppeteerGetAttributeValueAll(BotData data, FindElementBy findBy, string identifier,
        string attributeName = "innerText")
    {
        data.Logger.LogHeader();

        var elemScript = GetElementsScript(findBy, identifier);
        var frame = GetFrame(data);
        var script = $"Array.prototype.slice.call({elemScript}).map((item) => item.{attributeName})";
        var values = await frame.EvaluateExpressionAsync<string[]>(script);

        data.Logger.Log($"Got {values.Length} values for attribute {attributeName} by executing {script}", LogColors.DarkSalmon);
        return values.ToList();
    }

    /// <summary>
    /// Checks if an element is currently displayed on the page.
    /// </summary>
    [Block("Checks if an element is currently being displayed on the page", name = "Is Displayed")]
    public static async Task<bool> PuppeteerIsDisplayed(BotData data, FindElementBy findBy, string identifier, int index)
    {
        data.Logger.LogHeader();

        var elemScript = GetElementScript(findBy, identifier, index);
        var frame = GetFrame(data);
        var script = $"window.getComputedStyle({elemScript}).display !== 'none';";
        var displayed = await frame.EvaluateExpressionAsync<bool>(script);

        data.Logger.Log($"Found out the element is{(displayed ? "" : " not")} displayed by executing {script}", LogColors.DarkSalmon);
        return displayed;
    }

    /// <summary>
    /// Checks if an element exists on the page.
    /// </summary>
    [Block("Checks if an element exists on the page", name = "Exists")]
    public static async Task<bool> PuppeteerExists(BotData data, FindElementBy findBy, string identifier, int index)
    {
        data.Logger.LogHeader();

        var elemScript = GetElementScript(findBy, identifier, index);
        var frame = GetFrame(data);
        var script = $"window.getComputedStyle({elemScript}).display !== 'none';";

        try
        {
            await frame.EvaluateExpressionAsync<bool>(script);
            data.Logger.Log("The element exists", LogColors.DarkSalmon);
            return true;
        }
        catch
        {
            data.Logger.Log("The element does not exist", LogColors.DarkSalmon);
            return false;
        }
    }

    /// <summary>
    /// Uploads one or more files to the selected element.
    /// </summary>
    [Block("Uploads one or more files to the selected element", name = "Upload Files")]
    public static async Task PuppeteerUploadFiles(BotData data, FindElementBy findBy, string identifier, int index, List<string> filePaths)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        await elem.UploadFileAsync(filePaths.ToArray());

        data.Logger.Log($"Uploaded {filePaths.Count} files to the element", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Gets the X coordinate of the element in pixels.
    /// </summary>
    [Block("Gets the X coordinate of the element in pixels", name = "Get Position X")]
    public static async Task<int> PuppeteerGetPositionX(BotData data, FindElementBy findBy, string identifier, int index)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        var x = (int)(await elem.BoundingBoxAsync()).X;

        data.Logger.Log($"The X coordinate of the element is {x}", LogColors.DarkSalmon);
        return x;
    }

    /// <summary>
    /// Gets the Y coordinate of the element in pixels.
    /// </summary>
    [Block("Gets the Y coordinate of the element in pixels", name = "Get Position Y")]
    public static async Task<int> PuppeteerGetPositionY(BotData data, FindElementBy findBy, string identifier, int index)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        var y = (int)(await elem.BoundingBoxAsync()).Y;

        data.Logger.Log($"The Y coordinate of the element is {y}", LogColors.DarkSalmon);
        return y;
    }

    /// <summary>
    /// Gets the width of the element in pixels.
    /// </summary>
    [Block("Gets the width of the element in pixels", name = "Get Width")]
    public static async Task<int> PuppeteerGetWidth(BotData data, FindElementBy findBy, string identifier, int index)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        var width = (int)(await elem.BoundingBoxAsync()).Width;

        data.Logger.Log($"The width of the element is {width}", LogColors.DarkSalmon);
        return width;
    }

    /// <summary>
    /// Gets the height of the element in pixels.
    /// </summary>
    [Block("Gets the height of the element in pixels", name = "Get Height")]
    public static async Task<int> PuppeteerGetHeight(BotData data, FindElementBy findBy, string identifier, int index)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        var height = (int)(await elem.BoundingBoxAsync()).Height;

        data.Logger.Log($"The height of the element is {height}", LogColors.DarkSalmon);
        return height;
    }

    /// <summary>
    /// Takes a screenshot of the element and saves it to an output file.
    /// </summary>
    [Block("Takes a screenshot of the element and saves it to an output file", name = "Screenshot Element")]
    public static async Task PuppeteerScreenshotElement(BotData data, FindElementBy findBy, string identifier, int index,
        string fileName, bool fullPage = false, bool omitBackground = false)
    {
        data.Logger.LogHeader();
        // Element screenshots no longer support a FullPage option in PuppeteerSharp, keep the parameter for block compatibility.
        _ = fullPage;

        if (data.Providers.Security.RestrictBlocksToCWD)
            FileUtils.ThrowIfNotInCWD(fileName);

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        var page = GetPage(data);
        var options = await BuildElementScreenshotOptions(elem, omitBackground);
        await page.ScreenshotAsync(fileName, options);

        data.Logger.Log($"Took a screenshot of the element and saved it to {fileName}", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Takes a screenshot of the element and converts it to a base64 string.
    /// </summary>
    [Block("Takes a screenshot of the element and converts it to a base64 string", name = "Screenshot Element Base64")]
    public static async Task<string> PuppeteerScreenshotBase64(BotData data, FindElementBy findBy, string identifier, int index,
        bool fullPage = false, bool omitBackground = false)
    {
        data.Logger.LogHeader();
        // Element screenshots no longer support a FullPage option in PuppeteerSharp, keep the parameter for block compatibility.
        _ = fullPage;

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        var page = GetPage(data);
        var options = await BuildElementScreenshotOptions(elem, omitBackground);
        var base64 = await page.ScreenshotBase64Async(options);

        data.Logger.Log("Took a screenshot of the element as base64", LogColors.DarkSalmon);
        return base64;
    }

    /// <summary>
    /// Switches to a different iframe.
    /// </summary>
    [Block("Switches to a different iframe", name = "Switch to Frame")]
    public static async Task PuppeteerSwitchToFrame(BotData data, FindElementBy findBy, string identifier, int index)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var elem = await GetElement(frame, findBy, identifier, index);
        data.SetObject("puppeteerFrame", await elem.ContentFrameAsync());

        data.Logger.Log("Switched to iframe", LogColors.DarkSalmon);
    }

    /// <summary>
    /// Waits for an element to appear on the page.
    /// </summary>
    [Block("Waits for an element to appear on the page", name = "Wait for Element")]
    public static async Task PuppeteerWaitForElement(BotData data, FindElementBy findBy, string identifier, bool hidden = false, bool visible = true,
        int timeout = 30000)
    {
        data.Logger.LogHeader();

        var frame = GetFrame(data);
        var options = new WaitForSelectorOptions { Hidden = hidden, Visible = visible, Timeout = timeout };

        if (findBy == FindElementBy.XPath)
        {
            await frame.WaitForXPathAsync(identifier, options);
        }
        else
        {
            await frame.WaitForSelectorAsync(BuildSelector(findBy, identifier), options);
        }

        data.Logger.Log($"Waited for element with {findBy} {identifier}", LogColors.DarkSalmon);
    }

    private static async Task<IElementHandle> GetElement(IFrame frame, FindElementBy findBy, string identifier, int index)
    {
        var elements = findBy is FindElementBy.XPath ?
            await frame.XPathAsync(identifier) :
            await frame.QuerySelectorAllAsync(BuildSelector(findBy, identifier));

        if (elements.Length < index + 1)
        {
            throw new BlockExecutionException($"Expected at least {index + 1} elements to be found but {elements.Length} were found");
        }

        return elements[index];
    }

    private static string GetElementsScript(FindElementBy findBy, string identifier)
    {
        if (findBy == FindElementBy.XPath)
        {
            var script = $"document.evaluate({ToJavaScriptStringLiteral(identifier)}, document, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null)";
            return $"Array.from({{ length: {script}.snapshotLength }}, (_, index) => {script}.snapshotItem(index))";
        }

        return $"document.querySelectorAll({ToJavaScriptStringLiteral(BuildSelector(findBy, identifier))})";
    }

    private static string GetElementScript(FindElementBy findBy, string identifier, int index)
        => findBy == FindElementBy.XPath
            ? $"document.evaluate({ToJavaScriptStringLiteral(identifier)}, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue"
            : $"document.querySelectorAll({ToJavaScriptStringLiteral(BuildSelector(findBy, identifier))})[{index}]";

    private static string BuildSelector(FindElementBy findBy, string identifier)
        => findBy switch
        {
            FindElementBy.Id => '#' + identifier,
            FindElementBy.Class => '.' + string.Join('.', identifier.Split(' ')), // "class1 class2" => ".class1.class2"
            FindElementBy.Selector => identifier,
            _ => throw new NotSupportedException()
        };

    private static string ToJavaScriptStringLiteral(string value)
        => $"'{value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n")}'";

    private static IBrowser GetBrowser(BotData data)
        => data.TryGetObject<IBrowser>("puppeteer") ?? throw new BlockExecutionException("The browser is not open!");

    private static IPage GetPage(BotData data)
        => data.TryGetObject<IPage>("puppeteerPage") ?? throw new BlockExecutionException("No pages open!");

    private static IFrame GetFrame(BotData data)
        => data.TryGetObject<IFrame>("puppeteerFrame") ?? GetPage(data).MainFrame;

    private static async Task<ScreenshotOptions> BuildElementScreenshotOptions(IElementHandle element, bool omitBackground)
    {
        await element.ScrollIntoViewAsync();
        var boundingBox = await element.BoundingBoxAsync() ?? throw new BlockExecutionException("Could not determine the element bounds");

        return new ScreenshotOptions
        {
            Clip = new Clip
            {
                X = boundingBox.X,
                Y = boundingBox.Y,
                Width = boundingBox.Width,
                Height = boundingBox.Height
            },
            CaptureBeyondViewport = false,
            FromSurface = false,
            OptimizeForSpeed = true,
            OmitBackground = omitBackground,
            Type = omitBackground ? ScreenshotType.Png : ScreenshotType.Jpeg,
            Quality = omitBackground ? null : 100
        };
    }
}
