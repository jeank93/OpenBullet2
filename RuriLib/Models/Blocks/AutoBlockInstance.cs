using RuriLib.Exceptions;
using RuriLib.Extensions;
using RuriLib.Helpers;
using RuriLib.Helpers.CSharp;
using RuriLib.Helpers.LoliCode;
using RuriLib.Models.Configs;
using RuriLib.Models.Variables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RuriLib.Models.Blocks;

/// <summary>
/// An instance of a block that was auto generated from exposed methods.
/// </summary>
public class AutoBlockInstance : BlockInstance
{
    private string outputVariable = "output";

    /// <summary>
    /// Gets or sets the output variable written by the block.
    /// </summary>
    public string OutputVariable
    {
        get => outputVariable;
        set => outputVariable = VariableNames.MakeValid(value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the output should be captured.
    /// </summary>
    public bool IsCapture { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the block should run in safe mode.
    /// </summary>
    public bool Safe { get; set; }

    /// <summary>
    /// Initializes an auto-generated block instance.
    /// </summary>
    /// <param name="descriptor">The descriptor that defines the block.</param>
    public AutoBlockInstance(AutoBlockDescriptor descriptor)
        : base(descriptor)
    {
        OutputVariable = descriptor.Id[..1].ToLower() + descriptor.Id[1..] + "Output";
    }

    /// <inheritdoc />
    public override string ToLC(bool printDefaultParams = false)
    {
        /*
         *   SettingName = "my value"
         *   SettingName = 0
         *   SettingName = @myVariable
         */

        using var writer = new LoliCodeWriter(base.ToLC(printDefaultParams));

        if (Safe)
        {
            writer.AppendLine("SAFE", 2);
        }

        var outVarKind = IsCapture ? "CAP" : "VAR";

        // Write the output variable
        if (Descriptor.ReturnType.HasValue)
        {
            writer.AppendLine($"=> {outVarKind} @{OutputVariable}", 2);
        }

        return writer.ToString();
    }

    /// <inheritdoc />
    public override void FromLC(ref string script, ref int lineNumber)
    {
        // First parse the options that are common to every BlockInstance
        base.FromLC(ref script, ref lineNumber);

        using var reader = new StringReader(script);

        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            lineNumber++;
            var lineCopy = line;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("SAFE"))
            {
                Safe = true;
            }
            else if (line.StartsWith("=>"))
            {
                try
                {
                    var match = Regex.Match(line, "^=> ([A-Za-z]{3}) (.*)$");
                    IsCapture = match.Groups[1].Value.Equals("CAP", StringComparison.OrdinalIgnoreCase);
                    OutputVariable = match.Groups[2].Value.Trim()[1..];
                }
                catch
                {
                    throw new LoliCodeParsingException(lineNumber,
                        $"The output variable declaration is in the wrong format: {lineCopy.TruncatePretty(50)}");
                }
            }
            else
            {
                try
                {
                    LoliCodeParser.ParseSetting(ref line, Settings, Descriptor);
                }
                catch (Exception ex)
                {
                    throw new LoliCodeParsingException(lineNumber,
                        $"Could not parse the setting: {lineCopy.TruncatePretty(50)} ({ex.Message})");
                }
            }
        }
    }

    /// <inheritdoc />
    public override string ToCSharp(List<string> declaredVariables, ConfigSettings settings)
    {
        // If disabled /* code here */

        /*
         * With return type:
         * var myVar = MethodName(data, param1, param2 ...);
         *
         * Async:
         * await MethodName(data, param1, param2 ...);
         *
         */

        using var writer = new StringWriter();

        // Safe mode, wrap method in try/catch but declare variable outside of it
        if (Safe)
        {
            // If not void, initialize the variable with default value
            // Only do this if we haven't declared the variable yet!
            if (Descriptor.ReturnType.HasValue && !declaredVariables.Contains(OutputVariable)
                && !OutputVariable.StartsWith("globals."))
            {
                if (!Disabled)
                {
                    declaredVariables.Add(OutputVariable);
                }

                writer.WriteLine($"{GetRuntimeReturnType()} {OutputVariable} = {GetDefaultReturnValue()};");
            }

            writer.WriteLine("try {");

            // Here we already know the variable exists so we just do the assignment
            if (Descriptor.ReturnType.HasValue)
            {
                writer.Write($"{OutputVariable} = ");
            }

            WriteMethod(writer);

            writer.WriteLine("} catch (Exception safeException) {");
            writer.WriteLine("data.ERROR = safeException.PrettyPrint();");
            writer.WriteLine("data.Logger.Log($\"[SAFE MODE] Exception caught and saved to data.ERROR: {data.ERROR}\", LogColors.Tomato); }");
        }
        else
        {
            // If not void, do variable assignment
            if (Descriptor.ReturnType.HasValue)
            {
                if (declaredVariables.Contains(OutputVariable) || OutputVariable.StartsWith("globals."))
                {
                    writer.Write($"{OutputVariable} = ");
                }
                else
                {
                    if (!Disabled)
                    {
                        declaredVariables.Add(OutputVariable);
                    }

                    writer.Write($"{GetRuntimeReturnType()} {OutputVariable} = ");
                }
            }

            WriteMethod(writer);
        }

        return writer.ToString();
    }

    private void WriteMethod(StringWriter writer)
    {
        var descriptor = (Descriptor as AutoBlockDescriptor)!;

        // If async, prepend the await keyword
        if (descriptor.Async)
        {
            writer.Write("await ");
        }

        // Append MethodName(data, param1, "param2", param3);
        List<string> parameters = ["data", .. Settings.Values.Select(CSharpWriter.FromSetting)];

        var methodName = string.IsNullOrWhiteSpace(descriptor.MethodName)
            ? descriptor.Id
            : descriptor.MethodName;

        writer.Write($"{methodName}({string.Join(", ", parameters)})");

        if (descriptor.Async)
        {
            writer.WriteLine(".ConfigureAwait(false);");
        }
        else
        {
            writer.WriteLine(";");
        }

        // If the block has a return type, log which variable was written
        if (Descriptor.ReturnType.HasValue)
        {
            writer.WriteLine($"data.LogVariableAssignment(nameof({OutputVariable}));");

            if (IsCapture)
            {
                writer.WriteLine($"data.MarkForCapture(nameof({OutputVariable}));");
            }
        }
    }

    // This is needed otherwise when we have blocks made in other plugins they might reference
    // types from different runtimes and our castings like .AsBool() or .AsInt() will throw a
    // RuntimeBinderException, so we cannot just write 'var' but we need to explicitly write the type.
    private string GetRuntimeReturnType() => Descriptor.ReturnType switch
    {
        VariableType.Bool => "bool",
        VariableType.ByteArray => "byte[]",
        VariableType.DictionaryOfStrings => "Dictionary<string, string>",
        VariableType.Float => "float",
        VariableType.Int => "int",
        VariableType.ListOfStrings => "List<string>",
        VariableType.String => "string",
        _ => throw new NotSupportedException()
    };

    private string GetDefaultReturnValue() => Descriptor.ReturnType switch
    {
        VariableType.Bool => "false",
        VariableType.ByteArray => "Array.Empty<byte>()",
        VariableType.DictionaryOfStrings => "new()",
        VariableType.Float => "0",
        VariableType.Int => "0",
        VariableType.ListOfStrings => "new()",
        VariableType.String => "string.Empty",
        _ => throw new NotSupportedException()
    };
}
