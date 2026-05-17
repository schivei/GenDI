using Microsoft.Extensions.DependencyInjection;

[assembly: GenDI.GenDICoveration(false)]

namespace GenDI.ReferenceLibrary;

[ServiceInjection]
public interface IReferencedContract { }

[Injectable<IReferencedContract>(ServiceLifetime.Singleton, Module = "Referenced")]
public sealed class ReferencedService : IReferencedContract;
