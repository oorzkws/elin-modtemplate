namespace ElinModTemplate.Extensions;

public static class TypeExtensions {
    public static MethodBase CompilerGeneratedMethod(this Type parentType, string partialName, Type[] childArgs) {
        var possibilities = parentType.GetNestedTypes(AccessTools.all)
                                      .Where(type => type.CustomAttributes.Any(a => a.AttributeType == typeof(CompilerGeneratedAttribute)))
                                      .SelectMany(AccessTools.GetDeclaredMethods)
                                      .Union(AccessTools.GetDeclaredMethods(parentType)
                                                        .Where(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(CompilerGeneratedAttribute))))
                                      .Where(method => method.Name.Contains(partialName));


        foreach (var method in possibilities) {
            var parameters = method.GetParameters().Where(p => p.ParameterType != method.DeclaringType).ToArray();
            if (parameters.Length != childArgs.Length) {
                continue;
            }
            var shouldContinue = false;
            for (int i = 0; i < parameters.Length; i++) {
                if (parameters[i].ParameterType != childArgs[i] && childArgs[i] != typeof(object)) {
                    shouldContinue = true;
                    break;
                }
            }
            if (shouldContinue) {
                continue;
            }
            return method;
        }

        throw new Exception($"Could not find compiler generated method: {partialName} in {parentType.AssemblyQualifiedName}");
    }
}