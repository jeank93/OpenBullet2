using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RuriLib.Exceptions;
using RuriLib.Extensions;
using RuriLib.Helpers.CSharp;
using RuriLib.Helpers.LoliCode;
using RuriLib.Models.Blocks.Custom.HttpRequest;
using RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;
using RuriLib.Models.Blocks.Parameters;
using RuriLib.Models.Blocks.Settings;
using RuriLib.Models.Configs;

namespace RuriLib.Models.Blocks.Custom;

/// <summary>
/// Block instance for the custom HTTP request block.
/// </summary>
public class HttpRequestBlockInstance(HttpRequestBlockDescriptor descriptor) : BlockInstance(descriptor)
{
    /// <summary>
    /// Gets or sets the request-parameter payload matching the selected request type.
    /// </summary>
    public RequestParams RequestParams { get; set; } = new StandardRequestParams();

    /// <summary>
    /// Gets or sets a value indicating whether safe mode is enabled.
    /// </summary>
    public bool Safe { get; set; }

    /// <inheritdoc />
    public override string ToLC(bool printDefaultParams = false)
    {
        /*
         *   TYPE:STANDARD
         *   "name=hello&value=hi"
         *   "application/x-www-form-urlencoded"
         *
         *   TYPE:RAW
         *   BASE64_DATA
         *   "application/octet-stream"
         *
         *   TYPE:BASICAUTH
         *   "myUser"
         *   "myPass"
         *
         *   TYPE:MULTIPART
         *   "myBoundary"
         *   CONTENT:STRING "name" "content" "content-type"
         *   CONTENT:RAW "name" BASE64_DATA "content-type"
         *   CONTENT:FILE "name" "fileName" "content-type"
         *
         */

        using var writer = new LoliCodeWriter(base.ToLC(printDefaultParams));

        if (Safe)
        {
            writer.AppendLine("SAFE", 2);
        }

        switch (RequestParams)
        {
            case StandardRequestParams x:
                writer
                    .AppendLine("TYPE:STANDARD", 2)
                    .AppendLine(LoliCodeWriter.GetSettingValue(x.Content), 2)
                    .AppendLine(LoliCodeWriter.GetSettingValue(x.ContentType), 2);
                break;

            case RawRequestParams x:
                writer
                    .AppendLine("TYPE:RAW", 2)
                    .AppendLine(LoliCodeWriter.GetSettingValue(x.Content), 2)
                    .AppendLine(LoliCodeWriter.GetSettingValue(x.ContentType), 2);
                break;

            case BasicAuthRequestParams x:
                writer
                    .AppendLine("TYPE:BASICAUTH", 2)
                    .AppendLine(LoliCodeWriter.GetSettingValue(x.Username), 2)
                    .AppendLine(LoliCodeWriter.GetSettingValue(x.Password), 2);
                break;

            case MultipartRequestParams x:
                writer
                    .AppendLine("TYPE:MULTIPART", 2)
                    .AppendLine(LoliCodeWriter.GetSettingValue(x.Boundary), 2);

                foreach (var content in x.Contents)
                {
                    switch (content)
                    {
                        case StringHttpContentSettingsGroup y:
                            writer
                                .AppendToken("CONTENT:STRING", 2)
                                .AppendToken(LoliCodeWriter.GetSettingValue(y.Name))
                                .AppendToken(LoliCodeWriter.GetSettingValue(y.Data))
                                .AppendLine(LoliCodeWriter.GetSettingValue(y.ContentType));
                            break;

                        case RawHttpContentSettingsGroup y:
                            writer
                                .AppendToken("CONTENT:RAW", 2)
                                .AppendToken(LoliCodeWriter.GetSettingValue(y.Name))
                                .AppendToken(LoliCodeWriter.GetSettingValue(y.Data))
                                .AppendLine(LoliCodeWriter.GetSettingValue(y.ContentType));
                            break;

                        case FileHttpContentSettingsGroup y:
                            writer
                                .AppendToken("CONTENT:FILE", 2)
                                .AppendToken(LoliCodeWriter.GetSettingValue(y.Name))
                                .AppendToken(LoliCodeWriter.GetSettingValue(y.FileName))
                                .AppendLine(LoliCodeWriter.GetSettingValue(y.ContentType));
                            break;
                    }
                }

                break;
        }

        return writer.ToString();
    }

    /// <inheritdoc />
    public override void FromLC(ref string script, ref int lineNumber)
    {
        /*
         *   TYPE:STANDARD
         *   "name=hello&value=hi"
         *   "application/x-www-form-urlencoded"
         *
         *   TYPE:RAW
         *   BASE64_DATA
         *   "application/octet-stream"
         *
         *   TYPE:BASICAUTH
         *   "myUser"
         *   "myPass"
         *
         *   TYPE:MULTIPART
         *   "myBoundary"
         *   CONTENT:STRING "name" "content" "content-type"
         *   CONTENT:RAW "name" BASE64_DATA "content-type"
         *   CONTENT:FILE "name" "fileName" "content-type"
         *
         */

        ArgumentNullException.ThrowIfNull(script);

        // First parse the options that are common to every BlockInstance
        base.FromLC(ref script, ref lineNumber);

        using var reader = new StringReader(script);

        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            var lineCopy = line;
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("SAFE", StringComparison.Ordinal))
            {
                Safe = true;
                continue;
            }

            if (line.StartsWith("TYPE:", StringComparison.Ordinal))
            {
                try
                {
                    var reqParams = Regex.Match(line, "TYPE:([A-Z]+)").Groups[1].Value;

                    switch (reqParams)
                    {
                        case "STANDARD":
                            var standardReqParams = new StandardRequestParams();

                            // Read one line to parse the content
                            line = ReadRequiredLine(reader, ref lineNumber, "Missing standard content definition").Trim();
                            lineCopy = line;
                            LoliCodeParser.ParseSettingValue(ref line, standardReqParams.Content, new StringParameter(string.Empty));

                            // Read another line to parse the content-type
                            line = ReadRequiredLine(reader, ref lineNumber, "Missing standard content type definition").Trim();
                            lineCopy = line;
                            LoliCodeParser.ParseSettingValue(ref line, standardReqParams.ContentType, new StringParameter(string.Empty));

                            RequestParams = standardReqParams;
                            break;

                        case "RAW":
                            var rawReqParams = new RawRequestParams();

                            // Read one line to parse the content
                            line = ReadRequiredLine(reader, ref lineNumber, "Missing raw content definition").Trim();
                            lineCopy = line;
                            LoliCodeParser.ParseSettingValue(ref line, rawReqParams.Content, new ByteArrayParameter(string.Empty));

                            // Read another line to parse the content-type
                            line = ReadRequiredLine(reader, ref lineNumber, "Missing raw content type definition").Trim();
                            lineCopy = line;
                            LoliCodeParser.ParseSettingValue(ref line, rawReqParams.ContentType, new StringParameter(string.Empty));

                            RequestParams = rawReqParams;
                            break;

                        case "BASICAUTH":
                            var basicAuthReqParams = new BasicAuthRequestParams();

                            // Read one line to parse the username
                            line = ReadRequiredLine(reader, ref lineNumber, "Missing basic auth username").Trim();
                            lineCopy = line;
                            LoliCodeParser.ParseSettingValue(ref line, basicAuthReqParams.Username, new StringParameter(string.Empty));

                            // Read another line to parse the password
                            line = ReadRequiredLine(reader, ref lineNumber, "Missing basic auth password").Trim();
                            lineCopy = line;
                            LoliCodeParser.ParseSettingValue(ref line, basicAuthReqParams.Password, new StringParameter(string.Empty));

                            RequestParams = basicAuthReqParams;
                            break;

                        case "MULTIPART":
                            var multipartReqParams = new MultipartRequestParams();

                            // Read one line to parse the boundary
                            line = ReadRequiredLine(reader, ref lineNumber, "Missing multipart boundary").Trim();
                            lineCopy = line;
                            LoliCodeParser.ParseSettingValue(ref line, multipartReqParams.Boundary, new StringParameter(string.Empty));

                            RequestParams = multipartReqParams;
                            break;

                        default:
                            throw new LoliCodeParsingException(lineNumber, $"Invalid type: {reqParams}");
                    }
                }
                catch (LoliCodeParsingException)
                {
                    throw;
                }
                catch
                {
                    throw new LoliCodeParsingException(lineNumber, $"Could not parse the setting: {lineCopy.TruncatePretty(50)}");
                }
            }
            else if (line.StartsWith("CONTENT:", StringComparison.Ordinal))
            {
                try
                {
                    if (RequestParams is not MultipartRequestParams multipart)
                    {
                        throw new FormatException();
                    }

                    var token = LineParser.ParseToken(ref line);
                    var tokenType = Regex.Match(token, "CONTENT:([A-Z]+)").Groups[1].Value;

                    switch (tokenType)
                    {
                        case "STRING":
                            var stringContent = new StringHttpContentSettingsGroup();
                            LoliCodeParser.ParseSettingValue(ref line, stringContent.Name, new StringParameter(string.Empty));
                            LoliCodeParser.ParseSettingValue(ref line, stringContent.Data, new StringParameter(string.Empty));
                            LoliCodeParser.ParseSettingValue(ref line, stringContent.ContentType, new StringParameter(string.Empty));
                            multipart.Contents.Add(stringContent);
                            break;

                        case "RAW":
                            var rawContent = new RawHttpContentSettingsGroup();
                            LoliCodeParser.ParseSettingValue(ref line, rawContent.Name, new StringParameter(string.Empty));

                            // HACK: Cache the line to prevent it from being modified by the parser
                            // if the parse fails, we can still use the original line to parse the content-type
                            var lineCopyCache = line;

                            // Since an empty byte array is serialized as an empty string
                            // (this needs to change in the future) if this parse fails it
                            // means we actually parsed the content-type string instead
                            try
                            {
                                LoliCodeParser.ParseSettingValue(ref line, rawContent.Data,
                                    new ByteArrayParameter(string.Empty));
                            }
                            catch
                            {
                                line = lineCopyCache;
                            }

                            LoliCodeParser.ParseSettingValue(ref line, rawContent.ContentType, new StringParameter(string.Empty));
                            multipart.Contents.Add(rawContent);
                            break;

                        case "FILE":
                            var fileContent = new FileHttpContentSettingsGroup();
                            LoliCodeParser.ParseSettingValue(ref line, fileContent.Name, new StringParameter(string.Empty));
                            LoliCodeParser.ParseSettingValue(ref line, fileContent.FileName, new StringParameter(string.Empty));
                            LoliCodeParser.ParseSettingValue(ref line, fileContent.ContentType, new StringParameter(string.Empty));
                            multipart.Contents.Add(fileContent);
                            break;

                        default:
                            throw new FormatException();
                    }
                }
                catch
                {
                    throw new LoliCodeParsingException(lineNumber, $"Could not parse the multipart content: {lineCopy.TruncatePretty(50)}");
                }
            }
            else
            {
                try
                {
                    LoliCodeParser.ParseSetting(ref line, Settings, Descriptor);
                }
                catch
                {
                    throw new LoliCodeParsingException(lineNumber, $"Could not parse the setting: {lineCopy.TruncatePretty(50)}");
                }
            }
        }
    }

    /// <inheritdoc />
    public override string ToCSharp(List<string> definedVariables, ConfigSettings settings)
    {
        ArgumentNullException.ThrowIfNull(definedVariables);
        ArgumentNullException.ThrowIfNull(settings);

        using var writer = new StringWriter();

        if (Safe)
        {
            writer.WriteLine("try {");
        }

        writer.Write("await ");

        switch (RequestParams)
        {
            case StandardRequestParams x:
                writer.Write("HttpRequestStandard(data, new StandardHttpRequestOptions { ");
                writer.Write("Content = " + CSharpWriter.FromSetting(x.Content) + ", ");
                writer.Write("ContentType = " + CSharpWriter.FromSetting(x.ContentType) + ", ");
                writer.Write("UrlEncodeContent = " + GetSettingValue("urlEncodeContent") + ", ");
                break;

            case RawRequestParams x:
                writer.Write("HttpRequestRaw(data, new RawHttpRequestOptions { ");
                writer.Write("Content = " + CSharpWriter.FromSetting(x.Content) + ", ");
                writer.Write("ContentType = " + CSharpWriter.FromSetting(x.ContentType) + ", ");
                break;

            case BasicAuthRequestParams x:
                writer.Write("HttpRequestBasicAuth(data, new BasicAuthHttpRequestOptions { ");
                writer.Write("Username = " + CSharpWriter.FromSetting(x.Username) + ", ");
                writer.Write("Password = " + CSharpWriter.FromSetting(x.Password) + ", ");
                break;

            case MultipartRequestParams x:
                writer.Write("HttpRequestMultipart(data, new MultipartHttpRequestOptions { ");
                writer.Write("Boundary = " + CSharpWriter.FromSetting(x.Boundary) + ", ");
                writer.Write("Contents = " + SerializeMultipart(x.Contents) + ", ");
                break;
        }

        writer.Write("Url = " + GetSettingValue("url") + ", ");
        writer.Write("Method = " + GetSettingValue("method") + ", ");
        writer.Write("AutoRedirect = " + GetSettingValue("autoRedirect") + ", ");
        writer.Write("MaxNumberOfRedirects = " + GetSettingValue("maxNumberOfRedirects") + ", ");
        writer.Write("ReadResponseContent = " + GetSettingValue("readResponseContent") + ", ");
        writer.Write("AbsoluteUriInFirstLine = " + GetSettingValue("absoluteUriInFirstLine") + ", ");
        writer.Write("HttpLibrary = " + GetSettingValue("httpLibrary") + ", ");
        writer.Write("SecurityProtocol = " + GetSettingValue("securityProtocol") + ", ");
        writer.Write("CustomCookies = " + GetSettingValue("customCookies") + ", ");
        writer.Write("CustomHeaders = " + GetSettingValue("customHeaders") + ", ");
        writer.Write("TimeoutMilliseconds = " + GetSettingValue("timeoutMilliseconds") + ", ");
        writer.Write("HttpVersion = " + GetSettingValue("httpVersion") + ", ");
        writer.Write("CodePagesEncoding = " + GetSettingValue("codePagesEncoding") + ", ");
        writer.Write("AlwaysSendContent = " + GetSettingValue("alwaysSendContent") + ", ");
        writer.Write("DecodeHtml = " + GetSettingValue("decodeHtml") + ", ");
        writer.Write("UseCustomCipherSuites = " + GetSettingValue("useCustomCipherSuites") + ", ");
        writer.Write("CustomCipherSuites = " + GetSettingValue("customCipherSuites") + " ");

        writer.WriteLine("}).ConfigureAwait(false);");

        if (Safe)
        {
            writer.WriteLine("} catch (Exception safeException) {");
            writer.WriteLine("data.ERROR = safeException.PrettyPrint();");
            writer.WriteLine("data.Logger.Log($\"[SAFE MODE] Exception caught and saved to data.ERROR: {data.ERROR}\", LogColors.Tomato); }");
        }

        return writer.ToString();
    }

    /// <inheritdoc />
    public override IEnumerable<StatementSyntax> ToSyntax(BlockSyntaxGenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var invocationStatement = SyntaxFactory.ExpressionStatement(BuildHttpRequestInvocationExpression());

        if (!Safe)
        {
            return [invocationStatement];
        }

        return
        [
            SyntaxFactory.TryStatement(
                SyntaxFactory.Block(invocationStatement),
                SyntaxFactory.List([BlockSyntaxFactory.CreateSafeModeCatchClause()]),
                null)
        ];
    }

    private static string ReadRequiredLine(StringReader reader, ref int lineNumber, string errorMessage)
    {
        var line = reader.ReadLine();
        lineNumber++;

        return line ?? throw new LoliCodeParsingException(lineNumber, errorMessage);
    }

    private string SerializeMultipart(List<HttpContentSettingsGroup> contents)
        => $"new List<MyHttpContent> {{ {string.Join(", ", contents.Select(SerializeContent))} }}";

    private string SerializeContent(HttpContentSettingsGroup content)
        => content switch
        {
            StringHttpContentSettingsGroup x =>
                $"new StringHttpContent({CSharpWriter.FromSetting(x.Name)}, {CSharpWriter.FromSetting(x.Data)}, {CSharpWriter.FromSetting(x.ContentType)})",
            RawHttpContentSettingsGroup x =>
                $"new RawHttpContent({CSharpWriter.FromSetting(x.Name)}, {CSharpWriter.FromSetting(x.Data)}, {CSharpWriter.FromSetting(x.ContentType)})",
            FileHttpContentSettingsGroup x =>
                $"new FileHttpContent({CSharpWriter.FromSetting(x.Name)}, {CSharpWriter.FromSetting(x.FileName)}, {CSharpWriter.FromSetting(x.ContentType)})",
            _ => throw new NotImplementedException()
        };

    private string GetSettingValue(string name)
        => CSharpWriter.FromSetting(Settings[name]);

    private ExpressionSyntax BuildHttpRequestInvocationExpression()
    {
        var (methodName, optionsTypeName, requestAssignments) = RequestParams switch
        {
            StandardRequestParams x => (
                "HttpRequestStandard",
                "StandardHttpRequestOptions",
                new List<ExpressionSyntax>
                {
                    CreatePropertyAssignment("Content", CSharpWriter.FromSettingSyntax(x.Content)),
                    CreatePropertyAssignment("ContentType", CSharpWriter.FromSettingSyntax(x.ContentType)),
                    CreatePropertyAssignment("UrlEncodeContent", CSharpWriter.FromSettingSyntax(Settings["urlEncodeContent"]))
                }),
            RawRequestParams x => (
                "HttpRequestRaw",
                "RawHttpRequestOptions",
                new List<ExpressionSyntax>
                {
                    CreatePropertyAssignment("Content", CSharpWriter.FromSettingSyntax(x.Content)),
                    CreatePropertyAssignment("ContentType", CSharpWriter.FromSettingSyntax(x.ContentType))
                }),
            BasicAuthRequestParams x => (
                "HttpRequestBasicAuth",
                "BasicAuthHttpRequestOptions",
                new List<ExpressionSyntax>
                {
                    CreatePropertyAssignment("Username", CSharpWriter.FromSettingSyntax(x.Username)),
                    CreatePropertyAssignment("Password", CSharpWriter.FromSettingSyntax(x.Password))
                }),
            MultipartRequestParams x => (
                "HttpRequestMultipart",
                "MultipartHttpRequestOptions",
                new List<ExpressionSyntax>
                {
                    CreatePropertyAssignment("Boundary", CSharpWriter.FromSettingSyntax(x.Boundary)),
                    CreatePropertyAssignment("Contents", BuildMultipartContentsExpression(x.Contents))
                }),
            _ => throw new NotSupportedException()
        };

        requestAssignments.AddRange(
        [
            CreatePropertyAssignment("Url", CSharpWriter.FromSettingSyntax(Settings["url"])),
            CreatePropertyAssignment("Method", CSharpWriter.FromSettingSyntax(Settings["method"])),
            CreatePropertyAssignment("AutoRedirect", CSharpWriter.FromSettingSyntax(Settings["autoRedirect"])),
            CreatePropertyAssignment("MaxNumberOfRedirects", CSharpWriter.FromSettingSyntax(Settings["maxNumberOfRedirects"])),
            CreatePropertyAssignment("ReadResponseContent", CSharpWriter.FromSettingSyntax(Settings["readResponseContent"])),
            CreatePropertyAssignment("AbsoluteUriInFirstLine", CSharpWriter.FromSettingSyntax(Settings["absoluteUriInFirstLine"])),
            CreatePropertyAssignment("HttpLibrary", CSharpWriter.FromSettingSyntax(Settings["httpLibrary"])),
            CreatePropertyAssignment("SecurityProtocol", CSharpWriter.FromSettingSyntax(Settings["securityProtocol"])),
            CreatePropertyAssignment("CustomCookies", CSharpWriter.FromSettingSyntax(Settings["customCookies"])),
            CreatePropertyAssignment("CustomHeaders", CSharpWriter.FromSettingSyntax(Settings["customHeaders"])),
            CreatePropertyAssignment("TimeoutMilliseconds", CSharpWriter.FromSettingSyntax(Settings["timeoutMilliseconds"])),
            CreatePropertyAssignment("HttpVersion", CSharpWriter.FromSettingSyntax(Settings["httpVersion"])),
            CreatePropertyAssignment("CodePagesEncoding", CSharpWriter.FromSettingSyntax(Settings["codePagesEncoding"])),
            CreatePropertyAssignment("AlwaysSendContent", CSharpWriter.FromSettingSyntax(Settings["alwaysSendContent"])),
            CreatePropertyAssignment("DecodeHtml", CSharpWriter.FromSettingSyntax(Settings["decodeHtml"])),
            CreatePropertyAssignment("UseCustomCipherSuites", CSharpWriter.FromSettingSyntax(Settings["useCustomCipherSuites"])),
            CreatePropertyAssignment("CustomCipherSuites", CSharpWriter.FromSettingSyntax(Settings["customCipherSuites"]))
        ]);

        var optionsObject = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(optionsTypeName))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression,
                SyntaxFactory.SeparatedList(requestAssignments)));

        var invocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(methodName),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("data")),
                SyntaxFactory.Argument(optionsObject)
            ])));

        return SyntaxFactory.AwaitExpression(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                invocation,
                SyntaxFactory.IdentifierName("ConfigureAwait")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))))));
    }

    private static AssignmentExpressionSyntax CreatePropertyAssignment(string name, ExpressionSyntax value)
        => SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(name),
            value);

    private static ExpressionSyntax BuildMultipartContentsExpression(List<HttpContentSettingsGroup> contents)
        => SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("List<MyHttpContent>"))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.CollectionInitializerExpression,
                SyntaxFactory.SeparatedList(contents.Select(BuildMultipartContentExpression))));

    private static ExpressionSyntax BuildMultipartContentExpression(HttpContentSettingsGroup content)
        => content switch
        {
            StringHttpContentSettingsGroup x => CreateMultipartContentExpression(
                "StringHttpContent",
                CSharpWriter.FromSettingSyntax(x.Name),
                CSharpWriter.FromSettingSyntax(x.Data),
                CSharpWriter.FromSettingSyntax(x.ContentType)),
            RawHttpContentSettingsGroup x => CreateMultipartContentExpression(
                "RawHttpContent",
                CSharpWriter.FromSettingSyntax(x.Name),
                CSharpWriter.FromSettingSyntax(x.Data),
                CSharpWriter.FromSettingSyntax(x.ContentType)),
            FileHttpContentSettingsGroup x => CreateMultipartContentExpression(
                "FileHttpContent",
                CSharpWriter.FromSettingSyntax(x.Name),
                CSharpWriter.FromSettingSyntax(x.FileName),
                CSharpWriter.FromSettingSyntax(x.ContentType)),
            _ => throw new NotSupportedException()
        };

    private static ExpressionSyntax CreateMultipartContentExpression(
        string typeName,
        params ExpressionSyntax[] arguments)
        => SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(typeName))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(arguments.Select(SyntaxFactory.Argument))));
}
