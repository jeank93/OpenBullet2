using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RuriLib.Helpers.Blocks;
using RuriLib.Models.Blocks;
using RuriLib.Models.Blocks.Custom;
using RuriLib.Models.Trees;
using RuriLib.Models.Variables;
using Xunit;

namespace RuriLib.Tests.Helpers.Blocks;

public class DescriptorsRepositoryTests
{
    [Fact]
    public void GetAs_ExistingDescriptor_ReturnsTypedDescriptor()
    {
        var repository = new DescriptorsRepository();

        var descriptor = repository.GetAs<HttpRequestBlockDescriptor>("HttpRequest");

        Assert.Equal("HttpRequest", descriptor.Id);
    }

    [Fact]
    public void GetAs_WrongType_ThrowsInvalidCastException()
    {
        var repository = new DescriptorsRepository();

        Assert.Throws<InvalidCastException>(() => repository.GetAs<LoliCodeBlockDescriptor>("ConstantString"));
    }

    [Fact]
    public void ToVariableType_TaskString_ReturnsString()
        => Assert.Equal(VariableType.String, DescriptorsRepository.ToVariableType(typeof(Task<string>)));

    [Fact]
    public void ToVariableType_InvalidType_Throws()
        => Assert.Throws<InvalidCastException>(() => DescriptorsRepository.ToVariableType(typeof(DateTime)));

    [Fact]
    public void GetAs_BlockIdOverride_UsesStableIdAndAsyncMethodName()
    {
        var repository = new DescriptorsRepository();

        var descriptor = repository.GetAs<AutoBlockDescriptor>("FileExists");

        Assert.Equal("FileExists", descriptor.Id);
        Assert.Equal("FileExistsAsync", descriptor.MethodName);
        Assert.True(descriptor.Async);
    }

    [Fact]
    public void AsTree_ContainsAutoBlockDescriptors()
    {
        var repository = new DescriptorsRepository();

        var tree = repository.AsTree();

        Assert.True(tree.IsRoot);
        Assert.NotEmpty(tree.SubCategories);
        Assert.Contains(Flatten(tree), descriptor => descriptor.Id == "ConstantString");
    }

    private static IEnumerable<BlockDescriptor> Flatten(CategoryTreeNode node)
        => node.Descriptors.Concat(node.SubCategories.SelectMany(Flatten));
}
