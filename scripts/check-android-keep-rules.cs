#!/usr/bin/env -S dotnet --

// Checks that src/Sentry/buildTransitive/sentry-proguard.cfg keeps every Java member that Sentry's Android
// assemblies call through a binding.
//
// Apps that run R8 without trimming get no keep rules generated for the Java members C# calls through bindings, so
// sentry-proguard.cfg is the only thing keeping them. R8 removes or inlines any class member nothing keeps, and the
// call then fails at run time with NoSuchMethodError or NoSuchFieldError (see #6019 and #6031).
//
// The check reads the built Sentry, Sentry.Maui and Sentry.Bindings.Android assemblies for every Android target
// framework in Directory.Build.props, follows every member they use from a binding assembly to the Java member the
// binding calls, and fails when no rule in the cfg keeps it, or when it can't tell which Java member a binding member
// calls. It doesn't rely on the keep rules .NET Android or the AndroidX packages add, which change between versions.
// Not checked:
// - io.sentry.** classes, as long as the cfg keeps them whole (the check fails if it doesn't)
// - the BCL, Mono.Android and Java.Interop: platform and runtime classes come from the device or are kept by .NET
//   Android's own config
//
// Limits: only member references are followed. A binding class used only as a type (JavaCast<T>, typeof) or a Java
// class looked up by name (JniEnvironment.Types.TryFindClass) isn't seen.
//
// Usage, from the repository root after building Sentry.Maui for the Android target frameworks (it runs
// `dotnet msbuild` to read them):
//   dotnet run scripts/check-android-keep-rules.cs -- [--configuration Release] [--verbose]
//
// --verbose lists every Java member the check found and whether the cfg keeps it.

using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.RegularExpressions;

var configuration = "Release";
var verbose = false;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--configuration" && i + 1 < args.Length)
    {
        configuration = args[++i];
    }
    else if (args[i] == "--verbose")
    {
        verbose = true;
    }
    else
    {
        Console.Error.WriteLine("Usage: check-android-keep-rules.cs [--configuration <Release|Debug>] [--verbose]");
        return 2;
    }
}

const string cfgPath = "src/Sentry/buildTransitive/sentry-proguard.cfg";
if (!File.Exists(cfgPath))
{
    Console.Error.WriteLine($"{cfgPath} not found. Run this from the repository root.");
    return 2;
}

KeepRules rules;
try
{
    rules = KeepRules.Parse(File.ReadAllLines(cfgPath));
}
catch (FormatException e)
{
    Console.Error.WriteLine($"{cfgPath}: {e.Message}");
    return 2;
}

if (!rules.KeepsWhole("io.sentry.android.core.SentryAndroid"))
{
    Console.Error.WriteLine($"{cfgPath} no longer keeps io.sentry.** whole. This check skips io.sentry classes, so it needs updating first.");
    return 2;
}

if (AndroidTargetFrameworks() is not { Count: > 0 } tfms)
{
    Console.Error.WriteLine("Couldn't read LatestAndroidTfm and PreviousAndroidTfm from Directory.Build.props.");
    return 2;
}

var targets = new[] { "Sentry", "Sentry.Maui", "Sentry.Bindings.Android" }
    .SelectMany(project => tfms.Select(tfm => (Project: project, Tfm: tfm, Dll: $"src/{project}/bin/{configuration}/{tfm}/{project}.dll")))
    .ToList();

if (targets.Where(t => !File.Exists(t.Dll)).Select(t => t.Dll).ToList() is { Count: > 0 } notBuilt)
{
    Console.Error.WriteLine($"Not built: {string.Join(", ", notBuilt)}. Build Sentry.Maui for {string.Join(" and ", tfms)} first.");
    return 2;
}

var missing = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
var unmapped = new SortedSet<string>(StringComparer.Ordinal);
var checkedCount = 0;
foreach (var (project, tfm, dll) in targets)
{
    var resolver = new AssemblyResolver(project, tfm, dll, configuration);
    using var scanned = new PEReader(File.OpenRead(dll));
    var md = scanned.GetMetadataReader();

    foreach (var handle in md.MemberReferences)
    {
        var reference = md.GetMemberReference(handle);
        if (!Metadata.TryGetTypeReference(md, reference.Parent, out var typeRef))
        {
            continue;
        }

        var assemblyName = Metadata.AssemblyOf(md, typeRef);
        if (assemblyName is null || IsNotABinding(assemblyName))
        {
            continue;
        }

        var memberName = md.GetString(reference.Name);
        var typeName = Metadata.FullName(md, typeRef);
        if (resolver.Find(assemblyName, typeName) is not { } binding)
        {
            Console.Error.WriteLine($"{project} ({tfm}) uses {typeName} from {assemblyName}, which couldn't be found next to {dll} or in its project's NuGet assets.");
            return 2;
        }

        if (binding.JavaMembers(memberName) is not { } javaMembers)
        {
            unmapped.Add($"{typeName}.{memberName} ({assemblyName}), used in {project} ({tfm})");
            continue;
        }

        foreach (var javaMember in javaMembers)
        {
            checkedCount++;
            var key = javaMember.ToString();
            var kept = rules.Keeps(javaMember);
            if (verbose)
            {
                Console.WriteLine($"{(kept ? "kept   " : "MISSING")} {key}  ({typeName}.{memberName} in {project} {tfm})");
            }
            if (!kept)
            {
                if (!missing.TryGetValue(key, out var users))
                {
                    missing[key] = users = new SortedSet<string>(StringComparer.Ordinal);
                }
                users.Add($"{typeName}.{memberName} in {project} ({tfm})");
            }
        }
    }
}

if (unmapped.Count > 0)
{
    Console.Error.WriteLine("Couldn't tell which Java member these binding members call, so the check can't say whether the cfg keeps it:");
    foreach (var member in unmapped)
    {
        Console.Error.WriteLine($"  {member}");
    }
    Console.Error.WriteLine("Teach the check how to read them, or call the Java member some other way.");
}

if (missing.Count > 0)
{
    Console.Error.WriteLine($"{cfgPath} doesn't keep these Java members, which Sentry calls through bindings:");
    foreach (var (member, users) in missing)
    {
        Console.Error.WriteLine($"  {member}");
        foreach (var user in users)
        {
            Console.Error.WriteLine($"      used by {user}");
        }
    }
    Console.Error.WriteLine("Add a -keep rule for each one, so that apps running R8 without trimming keep them.");
}

if (unmapped.Count > 0 || missing.Count > 0)
{
    return 1;
}

Console.WriteLine($"{cfgPath} keeps all {checkedCount} Java member uses found in {targets.Count} Android assemblies ({string.Join(", ", targets.Select(t => $"{t.Project} {t.Tfm}"))}).");
return 0;

static List<string>? AndroidTargetFrameworks()
{
    var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", ["msbuild", "Directory.Build.props", "-getProperty:LatestAndroidTfm", "-getProperty:PreviousAndroidTfm"])
    {
        RedirectStandardOutput = true,
    };
    using var process = System.Diagnostics.Process.Start(startInfo)!;
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        return null;
    }
    using var json = JsonDocument.Parse(output);
    var tfms = json.RootElement.GetProperty("Properties").EnumerateObject()
        .Select(p => p.Value.GetString())
        .Where(tfm => !string.IsNullOrEmpty(tfm))
        .Select(tfm => tfm!)
        .Distinct()
        .ToList();
    return tfms.Count == 2 ? tfms : null;
}

static bool IsNotABinding(string assembly) =>
    assembly is "mscorlib" or "netstandard" or "Mono.Android" or "Java.Interop" or "_Microsoft.Android.Resource.Designer"
    || assembly.StartsWith("System", StringComparison.Ordinal)
    || assembly.StartsWith("Microsoft.", StringComparison.Ordinal)
    || (assembly.StartsWith("Sentry", StringComparison.Ordinal) && assembly != "Sentry.Bindings.Android");

record JavaMember(string Class, string Name, string Descriptor)
{
    public bool IsMethod => Descriptor.StartsWith('(');

    public override string ToString() => $"{Class}.{Name}{(Descriptor.StartsWith('(') ? "" : ":")}{Descriptor}";
}

// The keep rules sentry-proguard.cfg uses. Anything else is a FormatException, so that a new kind of rule gets support
// here instead of being ignored. Only -keep is supported: -keepclassmembers doesn't keep the class itself, and a class
// that only C# uses has nothing else keeping it.
sealed class KeepRules
{
    private readonly List<(Regex Class, bool Whole, Func<JavaMember, bool> Members)> _rules = [];

    public static KeepRules Parse(string[] lines)
    {
        var rules = new KeepRules();
        var text = string.Join("\n", lines.Select(l => Regex.Replace(l, "#.*$", "")));
        var rule = new Regex(@"-keep\s+(?<type>class|enum|interface)\s+(?<name>[\w.$*]+)\s*\{(?<body>[^}]*)\}");
        var position = 0;
        foreach (Match m in rule.Matches(text))
        {
            EnsureBlank(text[position..m.Index]);
            position = m.Index + m.Length;

            var members = m.Groups["body"].Value.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            Func<JavaMember, bool> keeps = members switch
            {
                ["*"] => _ => true,
                ["<fields>"] => member => !member.IsMethod,
                _ => NamedMembers(members),
            };
            rules._rules.Add((ClassPattern(m.Groups["name"].Value), members is ["*"], keeps));
        }
        EnsureBlank(text[position..]);
        return rules;
    }

    public bool Keeps(JavaMember member) =>
        _rules.Any(r => r.Class.IsMatch(member.Class) && r.Members(member));

    public bool KeepsWhole(string javaClass) =>
        _rules.Any(r => r.Whole && r.Class.IsMatch(javaClass));

    private static void EnsureBlank(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            throw new FormatException($"unsupported keep rule: {text.Trim()}");
        }
    }

    // "-keep class io.sentry.** { *; }": ** matches across package separators, * doesn't.
    private static Regex ClassPattern(string pattern) =>
        new("^" + Regex.Escape(pattern).Replace(@"\*\*", ".+").Replace(@"\*", "[^.]+") + "$");

    // "public abstract void addObserver(androidx.lifecycle.LifecycleObserver)" keeps the addObserver methods, and
    // "public static int VALUE" the VALUE field. Methods match by name only, so a rule for one overload counts for all.
    private static Func<JavaMember, bool> NamedMembers(List<string> members)
    {
        var named = members.Select(member =>
        {
            var isMethod = member.Contains('(');
            var match = Regex.Match(isMethod ? member[..member.IndexOf('(')] : member, @"(?<name>[\w$<>]+)\s*$");
            return match.Success
                ? (Name: match.Groups["name"].Value, IsMethod: isMethod)
                : throw new FormatException($"unsupported member: {member}");
        }).ToHashSet();
        return member => named.Contains((member.Name, member.IsMethod));
    }
}

// Finds the assemblies a scanned assembly references: next to it, in its project's NuGet assets, or in the output of a
// referenced project.
sealed class AssemblyResolver(string project, string tfm, string dll, string configuration)
{
    private readonly Dictionary<string, BindingAssembly?> _opened = [];
    private readonly Lazy<Dictionary<string, string>> _assets = new(() => ReadAssets(project, tfm, configuration));

    public BindingType? Find(string assembly, string typeName) => Open(assembly)?.FindType(typeName);

    private BindingAssembly? Open(string assembly)
    {
        if (!_opened.TryGetValue(assembly, out var opened))
        {
            var local = Path.Combine(Path.GetDirectoryName(dll)!, assembly + ".dll");
            var path = File.Exists(local) ? local : _assets.Value.GetValueOrDefault(assembly);
            _opened[assembly] = opened = path is null ? null : new BindingAssembly(path);
        }
        return opened;
    }

    private static Dictionary<string, string> ReadAssets(string project, string tfm, string configuration)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var assetsPath = $"src/{project}/obj/project.assets.json";
        if (!File.Exists(assetsPath))
        {
            return result;
        }

        using var json = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var root = json.RootElement;
        var packageFolder = root.GetProperty("packageFolders").EnumerateObject().First().Name;
        var libraries = root.GetProperty("libraries");
        if (!root.GetProperty("targets").TryGetProperty(tfm, out var target))
        {
            target = root.GetProperty("targets").EnumerateObject()
                .FirstOrDefault(t => t.Name.StartsWith(tfm.Split('-')[0] + "-android", StringComparison.Ordinal)).Value;
        }
        if (target.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var entry in target.EnumerateObject())
        {
            var library = libraries.GetProperty(entry.Name);
            var type = entry.Value.GetProperty("type").GetString();
            if (type == "package" && entry.Value.TryGetProperty("compile", out var compile))
            {
                var folder = Path.Combine(packageFolder, library.GetProperty("path").GetString()!);
                foreach (var file in compile.EnumerateObject().Where(f => f.Name.EndsWith(".dll", StringComparison.Ordinal)))
                {
                    result.TryAdd(Path.GetFileNameWithoutExtension(file.Name), Path.Combine(folder, file.Name));
                }
            }
            else if (type == "project")
            {
                var name = entry.Name.Split('/')[0];
                var projectDir = Path.GetDirectoryName(Path.GetFullPath(Path.Combine($"src/{project}", library.GetProperty("path").GetString()!)))!;
                var output = Path.Combine(projectDir, "bin", configuration, tfm, name + ".dll");
                if (File.Exists(output))
                {
                    result.TryAdd(name, output);
                }
            }
        }
        return result;
    }
}

sealed class BindingAssembly
{
    private readonly MetadataReader _md;
    private readonly PEReader _pe;

    public BindingAssembly(string path)
    {
        _pe = new PEReader(File.OpenRead(path));
        _md = _pe.GetMetadataReader();
    }

    public BindingType? FindType(string fullName)
    {
        var parts = fullName.Split('/');
        foreach (var handle in _md.TypeDefinitions)
        {
            var type = _md.GetTypeDefinition(handle);
            if (!type.GetDeclaringType().IsNil || Metadata.FullName(_md, type) != parts[0])
            {
                continue;
            }
            foreach (var nested in parts.Skip(1))
            {
                var next = type.GetNestedTypes().Select(_md.GetTypeDefinition).Where(t => _md.GetString(t.Name) == nested).ToList();
                if (next.Count == 0)
                {
                    return null;
                }
                type = next[0];
            }
            return new BindingType(_md, _pe, type);
        }
        return null;
    }
}

sealed class BindingType(MetadataReader md, PEReader pe, TypeDefinition type)
{
    private static readonly Regex JniMethod = new(@"^(?<name>[\w$<>]+)\.(?<sig>\(.*\).+)$");
    private static readonly Regex JniField = new(@"^(?<name>[\w$]+)\.(?<sig>\[*(L[^;]+;|[ZBCSIJFD]))$");
    private static readonly Regex JniConstructor = new(@"^\(.*\)V$");

    // The Java members a binding member calls. Empty for io.sentry classes, which the cfg keeps whole. Null when the
    // check can't tell: the type has no Java name, or the member has no JNI string and no [Register].
    public IReadOnlyList<JavaMember>? JavaMembers(string memberName)
    {
        var javaClass = JavaTypeName();
        if (javaClass is null)
        {
            return null;
        }
        if (javaClass.StartsWith("io/sentry/", StringComparison.Ordinal))
        {
            return [];
        }
        javaClass = javaClass.Replace('/', '.');

        var result = new List<JavaMember>();
        foreach (var method in type.GetMethods().Select(md.GetMethodDefinition).Where(m => md.GetString(m.Name) == memberName))
        {
            var fromIl = JniStrings(method).ToList();
            if (fromIl.Count > 0)
            {
                result.AddRange(fromIl.Select(jni => new JavaMember(javaClass, jni.Name, jni.Descriptor)));
            }
            else if (Register(method.GetCustomAttributes()) is { } register)
            {
                result.Add(new JavaMember(javaClass, register.Name == ".ctor" ? "<init>" : register.Name, register.Signature ?? ""));
            }
        }

        // Interface members have no body, and the generator puts [Register] on an interface property, not its accessors.
        if (result.Count == 0)
        {
            foreach (var property in type.GetProperties().Select(md.GetPropertyDefinition))
            {
                var accessors = property.GetAccessors();
                var accessorNames = new[] { accessors.Getter, accessors.Setter }
                    .Where(a => !a.IsNil)
                    .Select(a => md.GetString(md.GetMethodDefinition(a).Name));
                if (accessorNames.Contains(memberName) && Register(property.GetCustomAttributes()) is { Signature: not null } register)
                {
                    result.Add(new JavaMember(javaClass, register.Name, register.Signature));
                }
            }
        }
        return result.Count > 0 ? result : null;
    }

    // "androidx/lifecycle/Lifecycle" from [Register] or, for Java.Interop-style bindings, [JniTypeSignature].
    private string? JavaTypeName()
    {
        if (Register(type.GetCustomAttributes()) is { } register)
        {
            return register.Name;
        }
        foreach (var handle in type.GetCustomAttributes())
        {
            var attribute = md.GetCustomAttribute(handle);
            if (Metadata.AttributeTypeName(md, attribute) == "Java.Interop.JniTypeSignatureAttribute")
            {
                var value = md.GetBlobReader(attribute.Value);
                value.ReadUInt16();
                return value.ReadSerializedString();
            }
        }
        return null;
    }

    // Binding bodies pass the JNI member to JniPeerMembers as a string: "get.()Landroidx/lifecycle/LifecycleOwner;",
    // "ON_STOP.Landroidx/lifecycle/Lifecycle$Event;", or "(Ljava/lang/String;)V" for a constructor.
    private IEnumerable<(string Name, string Descriptor)> JniStrings(MethodDefinition method)
    {
        if (method.RelativeVirtualAddress == 0)
        {
            yield break;
        }
        foreach (var literal in IL.StringLiterals(md, pe.GetMethodBody(method.RelativeVirtualAddress)))
        {
            if (JniMethod.Match(literal) is { Success: true } m)
            {
                yield return (m.Groups["name"].Value, m.Groups["sig"].Value);
            }
            else if (JniField.Match(literal) is { Success: true } f)
            {
                yield return (f.Groups["name"].Value, f.Groups["sig"].Value);
            }
            else if (JniConstructor.IsMatch(literal))
            {
                yield return ("<init>", literal);
            }
        }
    }

    private (string Name, string? Signature)? Register(CustomAttributeHandleCollection attributes)
    {
        foreach (var handle in attributes)
        {
            var attribute = md.GetCustomAttribute(handle);
            if (Metadata.AttributeTypeName(md, attribute) != "Android.Runtime.RegisterAttribute")
            {
                continue;
            }
            var value = md.GetBlobReader(attribute.Value);
            value.ReadUInt16();
            var name = value.ReadSerializedString();
            var signature = value.RemainingBytes > 2 ? value.ReadSerializedString() : null;
            return name is null ? null : (name, signature);
        }
        return null;
    }
}

static class IL
{
    private static readonly Dictionary<short, OperandType> Operands = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => o.Value, o => o.OperandType);

    public static IEnumerable<string> StringLiterals(MetadataReader md, MethodBodyBlock body)
    {
        var il = body.GetILReader();
        while (il.RemainingBytes > 0)
        {
            short code = il.ReadByte();
            if (code == 0xFE)
            {
                code = unchecked((short)(0xFE00 | il.ReadByte()));
            }
            var operand = Operands[code];
            if (code == OpCodes.Ldstr.Value)
            {
                yield return md.GetUserString(MetadataTokens.UserStringHandle(il.ReadInt32() & 0xFFFFFF));
                continue;
            }
            switch (operand)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar:
                    il.ReadByte();
                    break;
                case OperandType.InlineVar:
                    il.ReadUInt16();
                    break;
                case OperandType.InlineI8 or OperandType.InlineR:
                    il.ReadInt64();
                    break;
                case OperandType.InlineSwitch:
                    var count = il.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        il.ReadInt32();
                    }
                    break;
                default:
                    il.ReadInt32();
                    break;
            }
        }
    }
}

static class Metadata
{
    public static bool TryGetTypeReference(MetadataReader md, EntityHandle parent, out TypeReferenceHandle typeRef)
    {
        if (parent.Kind == HandleKind.TypeReference)
        {
            typeRef = (TypeReferenceHandle)parent;
            return true;
        }
        if (parent.Kind == HandleKind.TypeSpecification)
        {
            var signature = md.GetBlobReader(md.GetTypeSpecification((TypeSpecificationHandle)parent).Signature);
            if (signature.ReadSignatureHeader().RawValue == (byte)SignatureTypeCode.GenericTypeInstance)
            {
                signature.ReadByte();
                if (signature.ReadTypeHandle() is { Kind: HandleKind.TypeReference } generic)
                {
                    typeRef = (TypeReferenceHandle)generic;
                    return true;
                }
            }
        }
        typeRef = default;
        return false;
    }

    public static string? AssemblyOf(MetadataReader md, TypeReferenceHandle handle)
    {
        var scope = md.GetTypeReference(handle).ResolutionScope;
        return scope.Kind switch
        {
            HandleKind.AssemblyReference => md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)scope).Name),
            HandleKind.TypeReference => AssemblyOf(md, (TypeReferenceHandle)scope),
            _ => null,
        };
    }

    public static string FullName(MetadataReader md, TypeReferenceHandle handle)
    {
        var type = md.GetTypeReference(handle);
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return FullName(md, (TypeReferenceHandle)type.ResolutionScope) + "/" + md.GetString(type.Name);
        }
        return (type.Namespace.IsNil ? "" : md.GetString(type.Namespace) + ".") + md.GetString(type.Name);
    }

    public static string FullName(MetadataReader md, TypeDefinition type) =>
        (type.Namespace.IsNil ? "" : md.GetString(type.Namespace) + ".") + md.GetString(type.Name);

    public static string AttributeTypeName(MetadataReader md, CustomAttribute attribute) => attribute.Constructor.Kind switch
    {
        HandleKind.MemberReference when md.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent is { Kind: HandleKind.TypeReference } parent
            => FullName(md, (TypeReferenceHandle)parent),
        HandleKind.MethodDefinition
            => FullName(md, md.GetTypeDefinition(md.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType())),
        _ => "",
    };
}
