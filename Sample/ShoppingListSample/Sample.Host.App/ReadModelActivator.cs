using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Metadata;
using Troolio.Core.ReadModels;

namespace Sample.Host.App;

// Register after the silo's built-in actor services so linked-model reducers are initialized.
public sealed class ReadModelActivator(IGrainActivator inner) : IGrainActivator
{
    public object CreateInstance(IGrainContext context)
    {
        var grain = inner.CreateInstance(context);
        if (grain is IReadModelResolver resolver) resolver.Initialize(context.ActivationServices);
        return grain;
    }
    public ValueTask DisposeInstance(IGrainContext context, object grain) => inner.DisposeInstance(context, grain);
}

public static class ReadModelServices
{
    public static void AddShoppingReadModels(this IServiceCollection services)
        => services.AddSingleton<IConfigureGrainTypeComponents, ConfigureReadModelActivation>();
}

public sealed class ConfigureReadModelActivation : IConfigureGrainTypeComponents
{
    public void Configure(GrainType grainType, GrainProperties properties, GrainTypeSharedContext shared)
    {
        if (shared.GetComponent<IGrainActivator>() is { } inner && inner is not ReadModelActivator)
            shared.SetComponent<IGrainActivator>(new ReadModelActivator(inner));
    }
}
