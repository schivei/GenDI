namespace GenDI;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ServiceInjectionAttribute : Attribute
{
}
