namespace GenDI;

public interface IInjectable
{
}

public interface ISingletonInjectable : IInjectable
{
}

public interface IScopedInjectable : IInjectable
{
}

public interface ITransientInjectable : IInjectable
{
}
