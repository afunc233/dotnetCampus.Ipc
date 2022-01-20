﻿using System.Diagnostics;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotnetCampus.Ipc.SourceGenerators.Compiling;

/// <summary>
/// 提供 IPC 对象（契约接口）的语法和语义分析。
/// </summary>
[DebuggerDisplay("IpcPublic : {ContractType.Name,nq}")]
internal class IpcPublicCompilation
{
    /// <summary>
    /// IPC 对象文件的编译信息。
    /// </summary>
    private readonly CompilationUnitSyntax _compilationUnitSyntax;

    /// <summary>
    /// 整个项目的语义模型。
    /// </summary>
    private readonly SemanticModel _semanticModel;

    /// <summary>
    /// 创建 IPC 对象的语法和语义分析。
    /// </summary>
    /// <param name="syntaxTree">IPC 对象所在整个文件的语法树。</param>
    /// <param name="semanticModel">语义模型。</param>
    /// <param name="ipcType">IPC 对象的语义符号。</param>
    public IpcPublicCompilation(SyntaxTree syntaxTree, SemanticModel semanticModel,
        INamedTypeSymbol ipcType)
    {
        _compilationUnitSyntax = syntaxTree.GetCompilationUnitRoot();
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
        IpcType = ipcType ?? throw new ArgumentNullException(nameof(ipcType));
    }

    /// <summary>
    /// IPC 对象（即标记了 <see cref="IpcPublicAttribute"/> 的接口类型）的语义符号。
    /// </summary>
    public INamedTypeSymbol IpcType { get; }

    /// <summary>
    /// 获取 IPC 对象所在文件的全部 using。
    /// </summary>
    /// <returns>全部 using 组成的字符串。</returns>
    public string GetUsing()
    {
        var usingsSyntax = _compilationUnitSyntax.Usings;
        var usings = string.Join(Environment.NewLine, usingsSyntax.Select(x => x.ToString()));
        return usings;
    }

    /// <summary>
    /// 获取 IPC 对象文件的命名空间。
    /// </summary>
    /// <returns>IPC 真实类型的命名空间</returns>
    public string GetNamespace()
    {
        return IpcType.ContainingNamespace.ToString();
    }

    /// <summary>
    /// 查找 IPC 对象的所有成员。
    /// </summary>
    /// <returns>所有成员信息。</returns>
    public IEnumerable<(INamedTypeSymbol ipcType, ISymbol member)> EnumerateMembersByContractType()
    {
        var members = IpcType.AllInterfaces.SelectMany(x => x.GetMembers())
            .Concat(IpcType.GetMembers());
        foreach (var member in members)
        {
            if (member is IMethodSymbol method && method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
            {
                // 属性内的方法忽略。
                continue;
            }

            if (member is INamedTypeSymbol)
            {
                // 接口中如果定义有其他嵌套类型/枚举/接口/结构，也忽略。
                continue;
            }

            if (member is IEventSymbol eventSymbol)
            {
                // IPC 不支持事件。
                var eventSyntax = eventSymbol.TryGetMemberDeclaration();
                throw new DiagnosticException(
                    DIPC021_EventIsNotSupportedForIpcObject,
                    eventSyntax?.GetLocation(),
                    eventSymbol.Name);
            }

            yield return new(IpcType, member);
        }
    }

    /// <summary>
    /// 在一个语法树（单个文件）中查找所有的 IPC 对象（契约接口）。
    /// </summary>
    /// <param name="compilation">整个项目的编译信息。</param>
    /// <param name="syntaxTree">单个文件的语法树。</param>
    /// <param name="publicIpcObjectCompilations">如果找到了 IPC 对象，则此参数为此语法树中的所有 IPC 对象；如果没有找到，则为空集合。</param>
    /// <returns>如果找到了 IPC 类型，则返回 true；如果没有找到，则返回 false。</returns>
    public static bool TryFind(Compilation compilation, SyntaxTree syntaxTree,
        out IReadOnlyList<IpcPublicCompilation> publicIpcObjectCompilations)
    {
        var result = new List<IpcPublicCompilation>();
        var typeDeclarationSyntaxes = from node in syntaxTree.GetRoot().DescendantNodes()
                                      where node.IsKind(SyntaxKind.InterfaceDeclaration)
                                      select (InterfaceDeclarationSyntax) node;

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        foreach (var typeDeclarationSyntax in typeDeclarationSyntaxes)
        {
            if (semanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is { } typeSymbol
                && typeSymbol.GetAttributes().FirstOrDefault(x => string.Equals(
                     x.AttributeClass?.ToString(),
                     typeof(IpcPublicAttribute).FullName,
                     StringComparison.Ordinal)) is { } ipcPublicAttribute)
            {
                result.Add(new IpcPublicCompilation(syntaxTree, semanticModel, typeSymbol));
            }
        }

        publicIpcObjectCompilations = result;
        return publicIpcObjectCompilations.Count > 0;
    }
}
