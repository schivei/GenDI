using GenDI;
using Microsoft.Extensions.DependencyInjection;

namespace GenDI.ReferenceLibrary;

[ServiceInjection]
public interface IReferencedContract { }

[Injectable<IReferencedContract>(ServiceLifetime.Singleton, Module = "Referenced")]
public sealed class ReferencedService : IReferencedContract;
