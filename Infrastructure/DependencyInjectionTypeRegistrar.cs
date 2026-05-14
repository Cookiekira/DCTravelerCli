using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DCTravelCli.Infrastructure;

internal sealed class DependencyInjectionTypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    public void Register(Type service, Type implementation) => services.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) => services.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory) => services.AddSingleton(service, _ => factory());

    public ITypeResolver Build() => new DependencyInjectionTypeResolver(services.BuildServiceProvider());
}

internal sealed class DependencyInjectionTypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type) => type is null ? null : provider.GetService(type);

    public void Dispose()
    {
        if (provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
